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
using System.Threading;
using MinecraftProtocol.Compatible;
using NyaProxy.Configs.Rule;
using NyaProxy.Configs;
using MinecraftProtocol.DataType;
using System.Text;
using System.Net;
using System.Collections.Generic;

namespace NyaProxy.Bridges
{
    public partial class BlockingBridge : Bridge, ICompatible
    {
        /// <summary>
        /// 客户端在握手时使用的协议版本
        /// </summary>
        public virtual int ProtocolVersion { get; private set; }
        
        /// <summary>
        /// 客户端到代理端之间是否强制使用了于服务端到代理端不一致的压缩阈值
        /// </summary>
        public virtual bool OverCompression { get; private set; }

        /// <summary>
        /// 客户端到代理端的压缩阈值
        /// </summary>
        public virtual int ClientCompressionThreshold { get; private set; }

        /// <summary>
        /// 服务端到代理端的压缩阈值
        /// </summary>
        public virtual int ServerCompressionThreshold { get; private set; }

        /// <summary>
        /// 玩家信息
        /// </summary>
        public virtual IPlayer Player { get; private set; }

        /// <summary>
        /// 服务端是否发送了Forge的数据包
        /// </summary>
        public virtual bool IsForge { get; private set; }

        /// <summary>
        /// 客户端到代理端之间是否启用了正版加密
        /// </summary>
        public virtual bool IsOnlineMode { get; private set; }


        public virtual CryptoHandler CryptoHandler => _clientSocketListener.CryptoHandler;

        private RSA _rsaService;
        private readonly string _serverId = ""; //1.7开始默认是空的
        private byte[] _verifyToken;

       
        public Stage Stage { get; private set; }
        
        private IHostTargetRule _targetRule;
        private IPacketListener _serverSocketListener;
        private IPacketListener _clientSocketListener;
        
        private int _forgeCheckCount;
        private int _queueIndex;

        private HandshakePacket _handshakePacket;
        private LoginStartPacket _loginStartPacket;

        public BlockingBridge(HostConfig host, HandshakePacket handshakePacket, Socket source, Socket destination) : base(host, handshakePacket.ServerAddress, source, destination)
        {
            ProtocolVersion = -1;
            ClientCompressionThreshold = -1;
            ServerCompressionThreshold = -1;

            _handshakePacket = handshakePacket;
            _queueIndex = QueueIndex.Get();

            Stage = Stage.Handshake;

            //监听客户端发送给服务端的数据包
            _clientSocketListener = new PacketListener(Source, !NyaProxy.Config.EnableReceivePool);            
            _clientSocketListener.StopListen += (sender, e) => Break();
            _clientSocketListener.UnhandledException += SimpleExceptionHandle;

            //监听服务端发送给客户端的数据包
            _serverSocketListener = new PacketListener(Destination, !NyaProxy.Config.EnableReceivePool);
            _serverSocketListener.StopListen += (sender, e) => Break();
            _serverSocketListener.UnhandledException += SimpleExceptionHandle;
        }

        public override Bridge Build()
        {
            if(_handshakePacket.NextState == HandshakePacket.State.Login)
            {
                _serverSocketListener.PacketReceived += Disconnect;
                _clientSocketListener.PacketReceived += BeforeLoginStart;
                _clientSocketListener.Start(ListenToken.Token);
            }
            else
            {
                ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Source, Destination, Direction.ToServer, _handshakePacket.AsCompatible(-1, -1), DateTime.Now));
                _clientSocketListener.PacketReceived += (s, e) => { if (e.Packet == PacketType.Status.Client.Request) Stage = Stage.Status; };
                _clientSocketListener.PacketReceived += ClientPacketReceived;
                _serverSocketListener.PacketReceived += ServerPacketReceived;
                _clientSocketListener.Start(ListenToken.Token);
                _serverSocketListener.Start(ListenToken.Token);
            }
            return this;
        }

        public override void Break()
        {
            base.Break();
            _handshakePacket = null; 
            _loginStartPacket = null;
        }
        public virtual void Break(string reason)
        {
            if (Stage == Stage.Login)
                Enqueue(Source, new DisconnectLoginPacket(reason, ProtocolVersion).Pack(ClientCompressionThreshold));
            else
                Enqueue(Source, new DisconnectPacket(reason, ProtocolVersion).Pack(ClientCompressionThreshold));
            ListenToken.Cancel();
        }

        private void BeforeLoginStart(object sender, PacketReceivedEventArgs e)
        {
            _clientSocketListener.PacketReceived -= BeforeLoginStart;
            if (LoginStartPacket.TryRead(e.Packet, out LoginStartPacket lsp))
            {
                Stage = Stage.Login;
                LoginStartEventArgs lsea = new LoginStartEventArgs(lsp);
                EventUtils.InvokeCancelEvent(NyaProxy.LoginStart, Source, lsea);
                if (lsea.IsBlock)
                {
                    Break();
                    return;
                }
                _loginStartPacket = lsea.Packet;
                    
                _targetRule = Host.GetRule(lsp.PlayerName);
                IsOnlineMode    = _targetRule.Flags.HasFlag(ServerFlags.OnlineMode);
                OverCompression = _targetRule.CompressionThreshold != -1;
                ProtocolVersion = _targetRule.ProtocolVersion > 0 ? _targetRule.ProtocolVersion : _handshakePacket.ProtocolVersion;
                
                _clientSocketListener.ProtocolVersion = ProtocolVersion;
                _serverSocketListener.ProtocolVersion = ProtocolVersion;

                string forgeTag = ProtocolVersion >= ProtocolVersions.V1_13 ? "\0FML2\0" : "\0FML\0"; ///好像Forge是在1.13更换了协议的?
                switch (Host.ForwardMode)
                {
                    case ForwardMode.Default:
                        if (Host.Flags.HasFlag(ServerFlags.Forge) && !_handshakePacket.ServerAddress.Contains(forgeTag))
                            _handshakePacket.ServerAddress += forgeTag;
                        break;
                    case ForwardMode.BungeeCord:
                        _handshakePacket.ServerAddress = new StringBuilder(_handshakePacket.GetServerAddressOnly())
                            .Append($"\0{(Source._remoteEndPoint() as IPEndPoint).Address}")
                            .Append($"\0{(_targetRule.Flags.HasFlag(ServerFlags.OnlineMode) ? UUID.GetFromMojangAsync(lsea.Packet.PlayerName).Result : UUID.GetFromPlayerName(lsea.Packet.PlayerName))}")
                            .Append(_targetRule.Flags.HasFlag(ServerFlags.Forge) ? forgeTag : "\0").ToString();
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (IsOnlineMode)
                {
                    _rsaService = RSA.Create(1024);
                    _verifyToken = CryptoUtils.GenerateRandomNumber(4);
                    _clientSocketListener.PacketReceived += BeforeEncryptionResponse;
                    EncryptionRequestPacket encryptionRequest = new EncryptionRequestPacket(_serverId, _rsaService.ExportSubjectPublicKeyInfo(), _verifyToken, ProtocolVersion);
                    Enqueue(Source, encryptionRequest.Pack(-1));
                }
                else
                {
                    StartExchange();
                }
            }
            else
            {
                Break($"错误的的数据包：{e.Packet.Id}");
            }
        }

        private void BeforeEncryptionResponse(object sender, PacketReceivedEventArgs e)
        {
            //只检查一个包，收到一个数据包后就立刻从事件中取消掉
            _clientSocketListener.PacketReceived -= BeforeEncryptionResponse;
            try
            {
                string playerName = _loginStartPacket.PlayerName;
                if (EncryptionResponsePacket.TryRead(e.Packet, out EncryptionResponsePacket erp))
                {
                    if (!CollectionUtils.Compare(_verifyToken, _rsaService.Decrypt(erp.VerifyToken, RSAEncryptionPadding.Pkcs1)))
                    {
                        NyaProxy.Logger.Warn($"[{SessionId}]{playerName}发送至代理端的VerifyToken于代理端发送至{Player.Name}的不匹配");
                        Break("正版验证异常，请联系管理员");
                    }

                    byte[] sessionKey = _rsaService.Decrypt(erp.SharedSecret, RSAEncryptionPadding.Pkcs1);
                    if (YggdrasilService.HasJoinedAsync(playerName, CryptoUtils.GetServerHash(_serverId, sessionKey, _rsaService.ExportSubjectPublicKeyInfo())).Result != null)
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
            _serverSocketListener.Start(ListenToken.Token);

            if (OverCompression)
            {
                ClientCompressionThreshold = _targetRule.CompressionThreshold;
                _clientSocketListener.CompressionThreshold = ClientCompressionThreshold;
                Packet packet = new SetCompressionPacket(ClientCompressionThreshold, ProtocolVersion);
                Enqueue(Source, CryptoHandler.TryEncrypt(packet.Pack(-1)), packet);
            }

            ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().
                Setup(this, Source, Destination, Direction.ToServer, _handshakePacket.AsCompatible(ProtocolVersion, ServerCompressionThreshold), DateTime.Now));
            ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().
                Setup(this, Source, Destination, Direction.ToServer, _loginStartPacket.AsCompatible(ProtocolVersion, ServerCompressionThreshold), DateTime.Now));
        }

        private void BeforeLoginSuccess(object sender, PacketReceivedEventArgs e)
        {
            try
            {
                if (e.Packet == PacketType.Login.Server.EncryptionRequest)
                {
                    NyaProxy.Logger.Error($"[{SessionId}]服务器{base.Host}({Destination._remoteEndPoint()})开启了正版验证，请关闭或切换至直通模式");
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
                    EventUtils.InvokeCancelEvent(NyaProxy.LoginSuccess, this, eventArgs.Setup(this, Source, Destination, Direction.ToClient, e) as LoginSuccessEventArgs);
                    if (!eventArgs.IsBlock)
                    {
                        Stage = Stage.Play;
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
            if(++_forgeCheckCount > 32)
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
            if (Stage == Stage.Handshake || Stage == Stage.Status)
                return;

            if (OverCompression)
                e.Packet.CompressionThreshold = ClientCompressionThreshold;

            if (Stage == Stage.Play ? e.Packet == PacketType.Play.Server.Disconnect : e.Packet == PacketType.Login.Server.Disconnect)
            {
                e.Cancel(); //阻止数据包被继续传递

                ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Destination, Source, Direction.ToClient, e));
                ListenToken.Cancel();
            }
        }

        private void ClientPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (OverCompression)
                e.Packet.CompressionThreshold = ServerCompressionThreshold;

            if (Stage == Stage.Play && e.Packet == PacketType.Play.Client.ChatMessage)
                ReceiveQueues[_queueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Source, Destination, Direction.ToServer, e));
            else if (Stage == Stage.Play && NyaProxy.Channles.Count > 0 && e.Packet == PacketType.Play.Client.PluginChannel)
                ReceiveQueues[_queueIndex].Add(PluginChannleEventArgsPool.Rent().Setup(this, Source, Destination, Direction.ToServer, e));
            else
                ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Source, Destination, Direction.ToServer, e));
        }

        private void ServerPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (OverCompression)
                e.Packet.CompressionThreshold = ClientCompressionThreshold;

            if (Stage == Stage.Play
                && e.Packet == PacketType.Play.Server.ChatMessage
                || e.Packet == PacketType.Play.Server.SystemChatMessage
                || e.Packet == PacketType.Play.Server.PlayerChatMessage
                || e.Packet == PacketType.Play.Server.DisguisedChatMessage)
                ReceiveQueues[_queueIndex].Add(ChatEventArgsPool.Rent().Setup(this, Destination, Source, Direction.ToClient, e));
            else if (Stage == Stage.Play && NyaProxy.Channles.Count > 0 && e.Packet == PacketType.Play.Server.PluginChannel)
                ReceiveQueues[_queueIndex].Add(PluginChannleEventArgsPool.Rent().Setup(this, Destination, Source, Direction.ToClient, e));
            else
                ReceiveQueues[_queueIndex].Add(PacketEventArgsPool.Rent().Setup(this, Destination, Source, Direction.ToClient, e));
        }
    }
}
