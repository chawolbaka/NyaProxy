﻿using System;
using System.Threading;
using NyaProxy.API.Enum;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Utils;
using System.Net.Sockets;
using NyaProxy.API;
using NyaProxy.Configs;

namespace NyaProxy.Bridges
{
    public partial class BlockingBridge : Bridge
    {

        public virtual int ProtocolVersion { get; }
        
        public virtual bool OverCompression { get; private set; }

        public virtual int ClientCompressionThreshold { get; set; }
        public virtual int ServerCompressionThreshold { get; set; }

        public virtual IPlayer Player { get; set; }

        public virtual bool IsForge { get; set; }

        private enum States { Login, Play }
        private int _queueIndex, _forgeCheckCount;
        private States _state = States.Login;
        private IPacketListener _serverSocketListener;
        private IPacketListener _clientSocketListener;
        private CancellationTokenSource _listenerToken = new CancellationTokenSource();

        public BlockingBridge(HostConfig host, string handshakeAddress, Socket source, Socket destination, int protocolVersion) : base(host, handshakeAddress, source, destination)
        {
            OverCompression = ClientCompressionThreshold != -1;
            ClientCompressionThreshold = host.CompressionThreshold;
            ServerCompressionThreshold = -1;

            ProtocolVersion = protocolVersion;
            _listenerToken.Token.Register(Break);
            _queueIndex = GetQueueIndex();


            //监听客户端发送给服务端的数据包
            _clientSocketListener = new PacketListener(Source);
            _clientSocketListener.ProtocolVersion = ProtocolVersion;
            _clientSocketListener.PacketReceived += ClientPacketReceived;
            _clientSocketListener.StopListen += (sender, e) => Break();
            _clientSocketListener.UnhandledException += SimpleExceptionHandle;

            //监听服务端发送给客户端的数据包
            _serverSocketListener = new PacketListener(Destination);
            _serverSocketListener.ProtocolVersion = ProtocolVersion;
            _serverSocketListener.PacketReceived += BeforeLoginSuccess;
            _serverSocketListener.StopListen += (sender, e) => Break();
            _serverSocketListener.UnhandledException += SimpleExceptionHandle;
        }

        public override Bridge Build() => Build(null);
        public virtual Bridge Build(params Packet[] packets)
        {   
            if (packets != null)
            {
                foreach (var packet in packets)
                {
                    //这个方法不安全，有可能send的不全
                    Enqueue(Destination, packet.Pack(-1), packet);
                }
            }



            if (OverCompression)
            {
                _clientSocketListener.CompressionThreshold = ClientCompressionThreshold;
                //这边必须直接发送不能走下面的ServerPacketReceived，否则会因为上面设置了CompressionThreshold导致SetCompressionPacket在Pack的时候多一个字节。
                using Packet packet = new SetCompressionPacket(ClientCompressionThreshold, ProtocolVersion);
                Enqueue(Source, packet.Pack(-1));
            }
            _serverSocketListener.Start(_listenerToken.Token);
            _clientSocketListener.Start(_listenerToken.Token);


            return this;
        }

        private void Disconnect(object sender, PacketReceivedEventArgs e)
        {
            if (e.Packet == PacketType.Play.Server.Disconnect)
            {
                _listenerToken.Cancel();
            }
        } 

        private void BeforeLoginSuccess(object sender, PacketReceivedEventArgs e)
        {
            try
            {
                if (e.Packet == PacketType.Login.Server.Disconnect)
                {
                    Span<Memory<byte>> rawData = e.RawData.Span;
                    for (int i = 0; i < rawData.Length; i++)
                    {
                        Enqueue(Source, rawData[i], i + 1 < e.RawData.Length ? null : e);
                    }
                    _listenerToken.Cancel(); return;
                }
                else if (SetCompressionPacket.TryRead(e.Packet, out SetCompressionPacket scp))
                {
                    ServerCompressionThreshold = scp.Threshold;
                    _serverSocketListener.CompressionThreshold = ServerCompressionThreshold;
                    if (!OverCompression)
                        _clientSocketListener.CompressionThreshold = ServerCompressionThreshold;
                    else //如果是接管了数据包压缩那么就把服务端发给客户端的SetCompression拦下来。
                        return;
                }
                else if (LoginSuccessPacket.TryRead(e.Packet, out LoginSuccessPacket lsp))
                {
                    Player = new BlockingBridgePlayer(this, lsp.PlayerUUID, lsp.PlayerName);
                    LoginSuccessEventArgs eventArgs = new LoginSuccessEventArgs();
                    EventUtils.InvokeCancelEvent(NyaProxy.LoginSuccess, this, eventArgs.Setup(this, Source, Direction.ToClient, e) as LoginSuccessEventArgs);
                    if (!eventArgs.IsBlock)
                    {
                        _serverSocketListener.PacketReceived -= BeforeLoginSuccess;
                        _serverSocketListener.PacketReceived += Disconnect;
                        _serverSocketListener.PacketReceived += CheckForge;
                        _serverSocketListener.PacketReceived += ServerPacketReceived;
                        _state = States.Play;
                    }
                    else
                    {
                        Break();
                    }
                }

                ServerPacketReceived(sender, e);
            }
            catch (Exception ex)
            {
                NyaProxy.Logger.Exception(ex);
                _listenerToken.Cancel();
            }
        }

        private void CheckForge(object sender, PacketReceivedEventArgs e)
        {
            //仅检查前64个包内是否包含forge的频道
            if(++_forgeCheckCount > 64)
                _serverSocketListener.PacketReceived -= CheckForge;

            if (ServerPluginChannelPacket.TryRead(e.Packet, true, out ServerPluginChannelPacket spcp))
            {
                if(spcp.Channel is "REGISTER" or "FML|HS")
                {
                    _serverSocketListener.PacketReceived -= CheckForge;
                    IsForge = true;
                }    
            }
        }

        private void ClientPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (OverCompression)
                e.Packet.CompressionThreshold = ServerCompressionThreshold;

            if (_state == States.Play && e.Packet == PacketType.Play.Client.ChatMessage)
                ReceiveQueues[_queueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Destination, Direction.ToServer, e));
            else if (NyaProxy.Channles.Count > 0 && _state == States.Play && e.Packet == PacketType.Play.Client.PluginChannel)
                ReceiveQueues[_queueIndex].Add(PluginChannleEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
            else
                ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Destination, Direction.ToServer, e));
        }

        private void ServerPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (OverCompression)
                e.Packet.CompressionThreshold = ClientCompressionThreshold;

            if (_state == States.Play && e.Packet == PacketType.Play.Server.ChatMessage)
                ReceiveQueues[_queueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
            else if (NyaProxy.Channles.Count > 0 && _state == States.Play && e.Packet == PacketType.Play.Server.PluginChannel)
                ReceiveQueues[_queueIndex].Add(PluginChannleEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
            else
                ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
        }
    }
}