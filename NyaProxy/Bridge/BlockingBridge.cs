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
        enum States
        {
            Login,
            Play
        }
        private States State = States.Login;

        private CancellationTokenSource ListenerToken = new CancellationTokenSource();
        
        public virtual int ProtocolVersion { get; }
        
        public virtual int CompressionThreshold { get; private set; }

        public virtual IPlayer Player { get; set; }

        public virtual IServer Server { get; set; }

        private IPacketListener ServerSocketListener;
        private IPacketListener ClientSocketListener;
        private int QueueIndex;

        public BlockingBridge(Socket source, Socket destination, string host, int protocolVersion) : base(source, destination)
        {
            Server = new ServerInfo(host);
            CompressionThreshold = -1;
            ProtocolVersion = protocolVersion;
            ListenerToken.Token.Register(Break);
            QueueIndex = GetQueueIndex();
        }

        public override Bridge Build() => Build(null);
        public virtual Bridge Build(params Packet[] packets)
        {
            //监听服务端发送给客户端的数据包
            ServerSocketListener = new PacketListener(Destination);
            ServerSocketListener.ProtocolVersion = ProtocolVersion;
            ServerSocketListener.PacketReceived += BeforeLoginSuccess;
            ServerSocketListener.StopListen += (sender, e) => Break();
            ServerSocketListener.UnhandledException += SimpleExceptionHandle;
            ServerSocketListener.Start(ListenerToken.Token);

            if (packets != null)
            {
                foreach (var packet in packets)
                {
                    //这个方法不安全，有可能send的不全
                    Destination.SendPacket(packet, -1);
                }
            }

            //监听客户端发送给服务端的数据包
            ClientSocketListener = new PacketListener(Source);
            ClientSocketListener.ProtocolVersion = ProtocolVersion;
            ClientSocketListener.PacketReceived += ClientPacketReceived;
            ClientSocketListener.StopListen += (sender, e) => Break();
            ClientSocketListener.UnhandledException += SimpleExceptionHandle;
            ClientSocketListener.Start(ListenerToken.Token);

            return this;
        }

        private void Disconnect(object sender, PacketListener.PacketReceivedEventArgs e)
        {
            if (e.Packet == PacketType.Play.Server.Disconnect)
            {
                ListenerToken.Cancel();
            }
        }
        private void BeforeLoginSuccess(object sender, PacketListener.PacketReceivedEventArgs e)
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
                    ListenerToken.Cancel(); return;
                }
                else if (e.Packet == PacketType.Login.Server.SetCompression)
                {
                    CompressionThreshold = e.Packet.AsSetCompression().Threshold;
                    ClientSocketListener.CompressionThreshold = CompressionThreshold;
                    ServerSocketListener.CompressionThreshold = CompressionThreshold;
                }
                else if (e.Packet == PacketType.Login.Server.LoginSuccess)
                {
                    LoginSuccessPacket lsp = e.Packet.AsLoginSuccess();
                    Player = new BlockingBridgePlayer(this, lsp.PlayerUUID, lsp.PlayerName);
                    LoginSuccessEventArgs eventArgs = new LoginSuccessEventArgs();
                    EventUtils.InvokeCancelEvent(NyaProxy.LoginSuccess, this, eventArgs.Setup(this, Source, Direction.ToClient, e) as LoginSuccessEventArgs);
                    if (!eventArgs.IsBlock)
                    {
                        ServerSocketListener.PacketReceived -= BeforeLoginSuccess;
                        ServerSocketListener.PacketReceived += Disconnect;
                        ServerSocketListener.PacketReceived += ServerPacketReceived;
                        State = States.Play;
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
                ListenerToken.Cancel();
            }
        }

        private void ClientPacketReceived(object sender, PacketListener.PacketReceivedEventArgs e)
        {
            if (State == States.Play && e.Packet == PacketType.Play.Client.ChatMessage)
                ReceiveQueues[QueueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Destination, Direction.ToServer, e));
            else
                ReceiveQueues[QueueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Destination, Direction.ToServer, e));
        }

        private void ServerPacketReceived(object sender, PacketListener.PacketReceivedEventArgs e)
        {
            if (State == States.Play && e.Packet == PacketType.Play.Server.ChatMessage)
                ReceiveQueues[QueueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
            else
                ReceiveQueues[QueueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Source, Direction.ToClient, e));
        }
    }
}
