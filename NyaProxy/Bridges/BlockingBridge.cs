using System;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Extension;
using MinecraftProtocol.IO;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Utils;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.Auth.Yggdrasil;


namespace NyaProxy.Bridges
{
    public partial class BlockingBridge : Bridge
    {
        public virtual int ProtocolVersion { get; }
        
        public virtual bool OverCompression { get; private set; }

        /// <summary>
        /// 客户端到代理端的压缩阈值
        /// </summary>
        public virtual int ClientCompressionThreshold { get; set; }

        /// <summary>
        /// 服务端到代理端的压缩阈值
        /// </summary>
        public virtual int ServerCompressionThreshold { get; set; }

        public virtual IPlayer Player { get; set; }

        public virtual bool IsForge { get; set; }

        /// <summary>
        /// 客户端到代理端之间是否启用了正版加密
        /// </summary>
        public virtual bool IsOnlineMode { get; set; }

        public virtual CryptoHandler CryptoHandler => _clientSocketListener.CryptoHandler;

        private readonly RSA _rsaService;
        private readonly byte[] _verifyToken;

        private enum States { Login, Play }
        private int _queueIndex;
        private int _forgeCheckCount;
        private States _state = States.Login;
        private IPacketListener _serverSocketListener;
        private IPacketListener _clientSocketListener;
        private Packet[] _loginToServerPackets; //一般里面是客户端发给服务端的握手包和开始登录包

        public BlockingBridge(IHostConfig host, string handshakeAddress, Socket source, Socket destination, int protocolVersion) : base(host, handshakeAddress, source, destination)
        {
            IsOnlineMode = host.Flags.HasFlag(ServerFlags.OnlineMode);
            if (IsOnlineMode)
            {
                _rsaService = RSA.Create(1024);
                _verifyToken = CryptoUtils.GenerateRandomNumber(4);
            }

            OverCompression = host.CompressionThreshold != -1;
            ClientCompressionThreshold = host.CompressionThreshold;
            ServerCompressionThreshold = -1;

            ProtocolVersion = protocolVersion;
            ListenToken.Token.Register(Break);
            _queueIndex = GetQueueIndex();


            //监听客户端发送给服务端的数据包
            _clientSocketListener = new PacketListener(Source, Source.ReceiveBufferSize, false);
            _clientSocketListener.ProtocolVersion = ProtocolVersion;
            _clientSocketListener.StopListen += (sender, e) => Break();
            _clientSocketListener.UnhandledException += SimpleExceptionHandle;

            //监听服务端发送给客户端的数据包
            _serverSocketListener = new PacketListener(Destination, Source.ReceiveBufferSize, false);
            _serverSocketListener.ProtocolVersion = ProtocolVersion;
            _serverSocketListener.StopListen += (sender, e) => Break();
            _serverSocketListener.PacketReceived += Disconnect;
            _serverSocketListener.UnhandledException += SimpleExceptionHandle;
        }

        public override Bridge Build() => Build(null);
        public virtual Bridge Build(params Packet[] packets)
        {
            _loginToServerPackets = packets;
            if (IsOnlineMode)
            {
                _clientSocketListener.PacketReceived += BeforeEncryptionResponse;
                _clientSocketListener.Start(ListenToken.Token);
                EncryptionRequestPacket encryptionRequest = new EncryptionRequestPacket("", _rsaService.ExportSubjectPublicKeyInfo(), _verifyToken, ProtocolVersion);
                Enqueue(Source, encryptionRequest.Pack(-1));
            }
            else
            {
                StartExchange();
            }

            return this;
        }

        public virtual void Break(string reason)
        {
            if (_state == States.Login)
                Enqueue(Source, new DisconnectLoginPacket(reason, ProtocolVersion).Pack(ClientCompressionThreshold));
            else
                Enqueue(Source, new DisconnectPacket(reason, ProtocolVersion).Pack(ClientCompressionThreshold));
            ListenToken.Cancel();
        }

        private void BeforeEncryptionResponse(object sender, PacketReceivedEventArgs e)
        {
            //只检查一个包，收到一个数据包后就立刻从事件中取消掉
            _clientSocketListener.PacketReceived -= BeforeEncryptionResponse;
            try
            {
                string playerName = (_loginToServerPackets.FirstOrDefault(x => x is LoginStartPacket) as LoginStartPacket).PlayerName;
                if (EncryptionResponsePacket.TryRead(e.Packet, out EncryptionResponsePacket erp))
                {
                    if (!CollectionUtils.Compare(_verifyToken, _rsaService.Decrypt(erp.VerifyToken, RSAEncryptionPadding.Pkcs1)))
                    {
                        NyaProxy.Logger.Warn($"[{SessionId}]{playerName}发送至代理端的VerifyToken于代理端发送至{Player.Name}的不匹配");
                        Break("正版验证异常，请联系管理员");
                    }

                    byte[] sessionKey = _rsaService.Decrypt(erp.SharedSecret, RSAEncryptionPadding.Pkcs1);
                    if (YggdrasilService.HasJoinedAsync(playerName, CryptoUtils.GetServerHash("", sessionKey, _rsaService.ExportSubjectPublicKeyInfo())).Result != null)
                    {
                        _clientSocketListener.CryptoHandler.Init(sessionKey);
                        StartExchange();
                    }
                    else
                    {
                        NyaProxy.Logger.Info($"[{SessionId}]玩家{playerName}没有通过Yggdrasil进行Join行为，该玩家可能未开启正版验证或网络有问题");
                        Break("正版验证失败，请检查你的网络。");
                    }
                }
                else
                {
                    NyaProxy.Logger.Error($"[{SessionId}]玩家{playerName}发送了加密响应以外的数据包");
                    NyaProxy.Logger.Unpreformat(e.Packet.ToString());
                    Break($"异常的数据包, 你可能使用了有问题的客户端。");
                }
            }
            catch (Exception ex)
            {
                NyaProxy.Logger.Exception(ex);
                Break();
            }
        }

        private void StartExchange()
        {
            _clientSocketListener.PacketReceived += ClientPacketReceived;
            _serverSocketListener.PacketReceived += BeforeLoginSuccess;
            if (!IsOnlineMode)
                _clientSocketListener.Start(ListenToken.Token);
            _serverSocketListener.Start(ListenToken.Token);

            if (OverCompression)
            {
                _clientSocketListener.CompressionThreshold = ClientCompressionThreshold;
                //这边必须直接发送不能走下面的ServerPacketReceived，否则会因为上面设置了CompressionThreshold导致SetCompressionPacket在Pack的时候多一个字节。
                using Packet packet = new SetCompressionPacket(ClientCompressionThreshold, ProtocolVersion);
                if (CryptoHandler.Enable)
                    Enqueue(Source, CryptoHandler.Encrypt(packet.Pack(-1)));
                else
                    Enqueue(Source, packet.Pack(-1));
            }

            if (_loginToServerPackets != null)
            {
                foreach (var packet in _loginToServerPackets)
                {
                    Enqueue(Destination, packet.Pack(-1), packet);
                }
                _loginToServerPackets = null;
            }


        }

        private void BeforeLoginSuccess(object sender, PacketReceivedEventArgs e)
        {
            try
            {
                if (e.Packet == PacketType.Login.Server.EncryptionRequest)
                {
                    NyaProxy.Logger.Error($"[{SessionId}]服务器{Host.Name}({Destination._remoteEndPoint()})开启了正版验证，请关闭或切换至直通模式");
                    Break("服务端配置异常，请问管理员查看日志");
                    return;
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
                        _state = States.Play;
                        _serverSocketListener.PacketReceived -= BeforeLoginSuccess;
                        _serverSocketListener.PacketReceived += CheckForge;
                        _serverSocketListener.PacketReceived += ServerPacketReceived;
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
                ListenToken.Cancel();
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

        private void Disconnect(object sender, PacketReceivedEventArgs e)
        {
            if (_state == States.Login ? e.Packet == PacketType.Login.Server.Disconnect : e.Packet == PacketType.Play.Server.Disconnect)
            {
                Span<Memory<byte>> rawData = e.RawData.Span;
                for (int i = 0; i < rawData.Length; i++)
                {
                    Enqueue(Source, rawData[i], i + 1 < e.RawData.Length ? null : e);
                }
                ListenToken.Cancel();
            }
        }

        private void ClientPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (OverCompression)
                e.Packet.CompressionThreshold = ServerCompressionThreshold;

            if (_state == States.Play && e.Packet == PacketType.Play.Client.ChatMessage)
                ReceiveQueues[_queueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Destination, Direction.ToServer, e));
            else if (NyaProxy.Channles.Count > 0 && _state == States.Play && e.Packet == PacketType.Play.Client.PluginChannel)
                ReceiveQueues[_queueIndex].Add(PluginChannleEventArgsPool.Rent().Setup(this, Source, Direction.ToServer, e));
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
