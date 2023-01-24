using System;
using System.Threading;
using NyaProxy.API.Enum;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Utils;
using System.Net.Sockets;
using NyaProxy.API;

namespace NyaProxy
{
    public partial class BlockingBridge : Bridge
    {

        public virtual int ProtocolVersion { get; }
        
        public virtual int CompressionThreshold { get; private set; }

        public virtual IPlayer Player { get; set; }

        public virtual bool IsForge { get; set; }

        private enum States { Login, Play }
        private int _queueIndex, _forgeCheckCount;
        private States _state = States.Login;
        private IPacketListener _serverSocketListener;
        private IPacketListener _clientSocketListener;
        private CancellationTokenSource _listenerToken = new CancellationTokenSource();
        private string _handshakeAddress;

        public BlockingBridge(HostConfig host, string handshakeAddress, Socket source, Socket destination, int protocolVersion) : base(host, handshakeAddress, source, destination)
        {
            CompressionThreshold = -1;
            ProtocolVersion = protocolVersion;
            _listenerToken.Token.Register(Break);
            _queueIndex = GetQueueIndex();
        }

        public override Bridge Build() => Build(null);
        public virtual Bridge Build(params Packet[] packets)
        {
            //监听服务端发送给客户端的数据包
            _serverSocketListener = new PacketListener(Destination);
            _serverSocketListener.ProtocolVersion = ProtocolVersion;
            _serverSocketListener.PacketReceived += BeforeLoginSuccess;
            _serverSocketListener.StopListen += (sender, e) => Break();
            _serverSocketListener.UnhandledException += SimpleExceptionHandle;
            _serverSocketListener.Start(_listenerToken.Token);

            if (packets != null)
            {
                foreach (var packet in packets)
                {
                    //这个方法不安全，有可能send的不全
                    Destination.SendPacket(packet, -1);
                }
            }

            //监听客户端发送给服务端的数据包
            _clientSocketListener = new PacketListener(Source);
            _clientSocketListener.ProtocolVersion = ProtocolVersion;
            _clientSocketListener.PacketReceived += ClientPacketReceived;
            _clientSocketListener.StopListen += (sender, e) => Break();
            _clientSocketListener.UnhandledException += SimpleExceptionHandle;
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
                else if (e.Packet == PacketType.Login.Server.SetCompression)
                {
                    CompressionThreshold = e.Packet.AsSetCompression().Threshold;
                    _clientSocketListener.CompressionThreshold = CompressionThreshold;
                    _serverSocketListener.CompressionThreshold = CompressionThreshold;
                }
                else if (e.Packet == PacketType.Login.Server.LoginSuccess)
                {
                    LoginSuccessPacket lsp = e.Packet.AsLoginSuccess();
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
            if (_state == States.Play && e.Packet == PacketType.Play.Client.ChatMessage)
                ReceiveQueues[_queueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Destination, Direction.ToServer, e));
            else if (NyaProxy.Channles.Count > 0 && _state == States.Play && e.Packet == PacketType.Play.Client.PluginChannel)
                ReceiveQueues[_queueIndex].Add(PluginChannleEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
            else
                ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Destination, Direction.ToServer, e));
        }

        private void ServerPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (_state == States.Play && e.Packet == PacketType.Play.Server.ChatMessage)
                ReceiveQueues[_queueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
            else if (NyaProxy.Channles.Count > 0 && _state == States.Play && e.Packet == PacketType.Play.Server.PluginChannel)
                ReceiveQueues[_queueIndex].Add(PluginChannleEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
            else
                ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
        }
    }
}
