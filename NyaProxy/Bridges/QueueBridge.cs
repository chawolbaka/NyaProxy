﻿using System;
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
using MinecraftProtocol.Compatible;
using NyaProxy.Configs.Rule;
using MinecraftProtocol.DataType;
using System.Text;
using System.Net;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Chat;
using MinecraftProtocol.IO.Pools;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NyaProxy.Configs;

namespace NyaProxy.Bridges
{
    public partial class QueueBridge : Bridge, ICompatible
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
        public virtual QueueBridgePlayer Player { get; private set; }

        /// <summary>
        /// 服务端是否发送了Forge的数据包
        /// </summary>
        public virtual bool IsForge { get; private set; }

        /// <summary>
        /// 客户端到代理端之间是否启用了正版加密
        /// </summary>
        public virtual bool IsOnlineMode { get; private set; }

        /// <summary>
        /// 1.19开始出现的东西，如果版本小于1.19该属性将为null
        /// </summary>
        public virtual ChatType[] ChatTypes { get; set; }

        public virtual CryptoHandler CryptoHandler => _clientSocketListener.CryptoHandler;

        public virtual Stage Stage { get; private set; }

    

        protected override string BreakMessage => Player != null ? $"§e{Player.Name} left the game" : base.BreakMessage;

        private BufferManager _buffer;

        private IHostTargetRule _targetRule;
        private IPacketListener _serverSocketListener;
        private IPacketListener _clientSocketListener;

        private RSA _rsaService;
        private readonly string _serverId = ""; //1.7开始默认是空的
        private byte[] _verifyToken;

        private int _forgeCheckCount;
        private int _queueIndex;

        private HandshakePacket _handshakePacket;
        private LoginStartPacket _loginStartPacket;

        public QueueBridge(Host host, HandshakePacket handshakePacket, Socket source, Socket destination) : this(GetNextSessionId(), host, handshakePacket, source, destination) { }
        internal QueueBridge(long sessionId, Host host, HandshakePacket handshakePacket, Socket source, Socket destination) : base(sessionId, host, handshakePacket.ServerAddress, source, destination)
        {
            if (NyaProxy.Config.EnableStickyPool)
                _buffer = new BufferManager(source, destination, NyaProxy.Network, NyaProxy.Config);

            ProtocolVersion = handshakePacket.ProtocolVersion;
            ClientCompressionThreshold = -1;
            ServerCompressionThreshold = -1;

            _handshakePacket = handshakePacket;
            _queueIndex = QueueIndex.Get();

            Stage = Stage.Handshake;

            //监听客户端发送给服务端的数据包
            _clientSocketListener = new PacketListener(Source, !NyaProxy.Config.EnableReceivePool);            
            _clientSocketListener.StopListen += (sender, e) => Break();
            _clientSocketListener.ProtocolVersion = ProtocolVersion;
            _clientSocketListener.UnhandledException += SimpleExceptionHandle;

            //监听服务端发送给客户端的数据包
            _serverSocketListener = new PacketListener(Destination, !NyaProxy.Config.EnableReceivePool);
            _serverSocketListener.StopListen += (sender, e) => Break();
            _serverSocketListener.ProtocolVersion = ProtocolVersion;
            _serverSocketListener.UnhandledException += SimpleExceptionHandle;
        }

        public override Bridge Build()
        {
            if(_handshakePacket.NextState == HandshakeState.Login)
            {
                _serverSocketListener.PacketReceived += Disconnect;
                _clientSocketListener.PacketReceived += BeforeLoginStart;
                _clientSocketListener.Start(ListenToken.Token);
            }
            else if(_handshakePacket.NextState == HandshakeState.GetStatus)
            {
                Enqueue(PacketEventArgsPool.Rent().Setup(this, Source, Destination, Direction.ToServer, _handshakePacket.AsCompatible(-1, -1), DateTime.Now));
                _clientSocketListener.PacketReceived += (s, e) => { if (e.Packet == PacketType.Status.Client.Request) Stage = Stage.Status; };
                _clientSocketListener.PacketReceived += ClientPacketReceived;
                _serverSocketListener.PacketReceived += ServerPacketReceived;
                _clientSocketListener.Start(ListenToken.Token);
                _serverSocketListener.Start(ListenToken.Token);
            }
            else
            {
                throw new InvalidOperationException($"Unknow handshake state {_handshakePacket.NextState}");
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
            if (Player != null)
                Player.KickAsync(reason);
            else if (Stage != Stage.Play)
                NyaProxy.Network.Enqueue(Source, new DisconnectLoginPacket(ChatComponent.Parse(reason), ProtocolVersion).Pack(ClientCompressionThreshold));
            else
                NyaProxy.Network.Enqueue(Source, new DisconnectPacket(reason, ProtocolVersion).Pack(ClientCompressionThreshold));
            ListenToken.Cancel();
        }

        private void BeforeLoginStart(object sender, PacketReceivedEventArgs e)
        {
            _clientSocketListener.PacketReceived -= BeforeLoginStart;
            try
            {
                if (LoginStartPacket.TryRead(e.Packet, out LoginStartPacket lsp))
                {
                    Stage = Stage.Login;
                    Player = new QueueBridgePlayer(this, lsp.PlayerUUID, lsp.PlayerName);
                    LoginStartEventArgs lsea = new LoginStartEventArgs(this, Source, Destination, Direction.ToServer, lsp, DateTime.Now);
                    NyaProxy.LoginStart.Invoke(this, lsea, NyaProxy.Logger);
                 
                    
                    if (lsea.IsBlock)
                    {
                        Break();
                        return;
                    }
                    NyaProxy.Logger.LogInformation($"{lsp.PlayerName}[{Source._remoteEndPoint()}] logged in with host {Host.Name}[{Destination._remoteEndPoint()}]");

                    _loginStartPacket = lsea.PacketCheaged ? lsea.Packet.Get().AsLoginStart() : lsp;

                    _targetRule = Host.GetRule(lsp.PlayerName);
                    IsOnlineMode = _targetRule.Flags.HasFlag(ServerFlags.OnlineMode);
                    OverCompression = _targetRule.CompressionThreshold != -1;
                    if (_targetRule.ProtocolVersion > 0)
                    {
                        ProtocolVersion = _targetRule.ProtocolVersion;
                        _clientSocketListener.ProtocolVersion = ProtocolVersion;
                        _serverSocketListener.ProtocolVersion = ProtocolVersion;
                    }

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
                                .Append($"\0{(_targetRule.Flags.HasFlag(ServerFlags.OnlineMode) ? UUID.GetFromMojangAsync(_loginStartPacket.PlayerName).Result : UUID.GetFromPlayerName(_loginStartPacket.PlayerName))}")
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
                        NyaProxy.Network.Enqueue(Source, encryptionRequest.Pack(-1));
                    }
                    else
                    {
                        StartExchange();
                    }
                }
                else
                {
                    NyaProxy.Logger.LogDebug(i18n.Debug.ReceivedEnexpectedPacketDuringLoginStart.Replace("{EndPoint}", Source._remoteEndPoint(), "{Packet}", e.Packet.Get()));
                    Break(i18n.Disconnect.ReceivedEnexpectedPacket.Replace("{EndPoint}", Source._remoteEndPoint(), "{PacketId}", e.Packet.Id));
                }
            }
            catch (Exception ex)
            {
                NyaProxy.Logger.LogError(ex);
                Break();
            }
            finally
            {
                e.Dispose();
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
                        NyaProxy.Logger.LogWarning(i18n.Warning.VerifyTokenNotMatch.Replace("{EndPoint}", Source._remoteEndPoint(), "{PlayerName}", playerName, "{Packet}", erp));
                        Break(i18n.Disconnect.AuthenticationFailed);
                    }

                    byte[] sessionKey = _rsaService.Decrypt(erp.SharedSecret, RSAEncryptionPadding.Pkcs1);
                    if (YggdrasilService.HasJoinedAsync(playerName, CryptoUtils.GetServerHash(_serverId, sessionKey, _rsaService.ExportSubjectPublicKeyInfo())).Result != null)
                    {
                        _clientSocketListener.CryptoHandler.Init(sessionKey);
                        StartExchange();
                    }
                    else
                    {
                        NyaProxy.Logger.LogDebug(i18n.Debug.PlayerNotJoinedThroughYggdrasil.Replace("{PlayerName}", playerName));
                        Break(i18n.Disconnect.AuthenticationFailed);
                    }
                }
                else
                {
                    Break(i18n.Disconnect.ReceivedEnexpectedPacket.Replace("{PacketId}", e.Packet.Id));
                    NyaProxy.Logger.LogDebug(i18n.Debug.ReceivedEnexpectedPacketDuringEncryption.Replace("{EndPoint}", Source._remoteEndPoint(), "{PlayerName}", playerName, "{Packet}", e.Packet));
                }
            }
            catch (Exception ex)
            {
                NyaProxy.Logger.LogError(ex);
                Break();
            }
            finally
            {
                e.Dispose();
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
                NyaProxy.Network.Enqueue(Source, CryptoHandler.TryEncrypt(packet.Pack(-1)), packet);
            }

            Enqueue(PacketEventArgsPool.Rent().
                Setup(this, Source, Destination, Direction.ToServer, _handshakePacket.AsCompatible(ProtocolVersion, ServerCompressionThreshold), DateTime.Now));
            Enqueue(PacketEventArgsPool.Rent().
                Setup(this, Source, Destination, Direction.ToServer, _loginStartPacket.AsCompatible(ProtocolVersion, ServerCompressionThreshold), DateTime.Now));
        }


        private void BeforeLoginSuccess(object sender, PacketReceivedEventArgs e)
        {
            try
            {
                if (e.Packet == PacketType.Login.Server.EncryptionRequest)
                {
                    NyaProxy.Logger.LogError(i18n.Error.OnlineModeNotTurned.Replace("{Host}", Host.Name));
                    e.Dispose(); 
                    Break(i18n.Disconnect.IncorrectServerConfiguration);
                    return;
                }
                else if (SetCompressionPacket.TryRead(e.Packet, out SetCompressionPacket scp))
                {
                    ServerCompressionThreshold = scp.Threshold;
                    _serverSocketListener.CompressionThreshold = ServerCompressionThreshold;
                    if (!OverCompression)
                        _clientSocketListener.CompressionThreshold = ServerCompressionThreshold;
                    else //如果是接管了数据包压缩那么就把服务端发给客户端的SetCompression拦下来。
                        { e.Dispose(); return; }
                }
                else if (LoginSuccessPacket.TryRead(e.Packet, out LoginSuccessPacket lsp))
                {
                    //防止和LoginStart的不同步，这边需要重新赋值一次
                    Player.Id = lsp.PlayerUUID;
                    Player.Name = lsp.PlayerName;
                    LoginSuccessEventArgs eventArgs = new LoginSuccessEventArgs();
                    NyaProxy.LoginSuccess.Invoke(this, eventArgs.Setup(this, Source, Destination, Direction.ToClient, e) as LoginSuccessEventArgs, NyaProxy.Logger);
                    if (!eventArgs.IsBlock)
                    {
                        Stage = Stage.Play;
                        _serverSocketListener.PacketReceived -= BeforeLoginSuccess;
                        _serverSocketListener.PacketReceived += ServerPacketReceived;
                        if (!Host.CompatibilityMode)
                        {
                            _serverSocketListener.PacketReceived += CheckForge;
                            if (ProtocolVersion >= ProtocolVersions.V1_19 && ProtocolVersion <= ProtocolVersions.V1_19_3)
                                _serverSocketListener.PacketReceived += WaitForGameJoin;
                        }
                        else
                        {
                            IsForge = Host.Flags.HasFlag(ServerFlags.Forge);
                        }
                    }
                    else
                    {
                        e.Dispose();
                        Break();
                        return;
                    }
                }
                ServerPacketReceived(sender, e);
            }
            catch (Exception ex)
            {
                NyaProxy.Logger.LogError(ex);
                ListenToken.Cancel();
            }
        }


        private void WaitForGameJoin(object sender, PacketReceivedEventArgs e)
        {
            if (JoinGamePacket.TryRead(e.Packet, out JoinGamePacket jgp))
            {
                _serverSocketListener.PacketReceived -= WaitForGameJoin;
                jgp.TryGetChatTypes(out ChatType[] chatTypes);
                ChatTypes = chatTypes;
                jgp.Dispose();
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
                e.Packet.Get().CompressionThreshold = ClientCompressionThreshold;

            if (Stage == Stage.Play ? e.Packet == PacketType.Play.Server.Disconnect : e.Packet == PacketType.Login.Server.Disconnect)
            {
                e.Cancel(); //阻止数据包被继续传递

                Enqueue(PacketEventArgsPool.Rent().Setup(this, Destination, Source, Direction.ToClient, e));
                ListenToken.Cancel();
            }
        }

        private void ClientPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (OverCompression)
                e.Packet.Get().CompressionThreshold = ServerCompressionThreshold;

            if (!Host.CompatibilityMode && Stage == Stage.Play && e.Packet == PacketType.Play.Client.ChatMessage)
                Enqueue(new ChatSendEventArgs(this, Source, Destination, Direction.ToServer, e));
            else
                Enqueue(PacketEventArgsPool.Rent().Setup(this, Source, Destination, Direction.ToServer, e));
        }

        private void ServerPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (OverCompression)
                e.Packet.Get().CompressionThreshold = ClientCompressionThreshold;

            if (!Host.CompatibilityMode && Stage == Stage.Play
                && e.Packet == PacketType.Play.Server.ChatMessage
                || e.Packet == PacketType.Play.Server.SystemChatMessage
                || e.Packet == PacketType.Play.Server.PlayerChatMessage
                || e.Packet == PacketType.Play.Server.DisguisedChatMessage)
                Enqueue(new ChatSendEventArgs(this, Destination, Source, Direction.ToClient, e));
            else
                Enqueue(PacketEventArgsPool.Rent().Setup(this, Destination, Source, Direction.ToClient, e));
        }

        private void Enqueue(PacketSendEventArgs psea)
        {
            if (EnableBlockingQueue)
                ReceiveBlockingQueues[_queueIndex].Add(psea);
            else
                ReceiveQueues[_queueIndex].Enqueue(psea);
        }

        private class BufferManager
        {
            private static Bucket<byte> StickyPool;
            private static Bucket<IDisposable> DisposablePool;

            public SocketSendBuffer Client { get; set; }
            public SocketSendBuffer Server { get; set; }

            private static readonly object _lock = new object();

            public BufferManager(Socket clientSocket, Socket serverSocket, NetworkHelper network, MainConfig config)
            {
                Bucket<byte> stickyPool;
                Bucket<IDisposable> disposablePool;
                lock (_lock)
                {
                    if (config.EnableStickyPool)
                    {
                        StickyPool ??= new Bucket<byte>(config.StickyPoolBufferLength, config.NumberOfStickyPoolBuffers, 233, true);
                        DisposablePool ??= new Bucket<IDisposable>(16, 512, 2333, true);
                    }
                    else
                    {
                        StickyPool = null;
                        DisposablePool = null;
                    }

                    stickyPool = StickyPool;
                    disposablePool = DisposablePool;
                }

                Client = new SocketSendBuffer(clientSocket, network, stickyPool, disposablePool);
                Server = new SocketSendBuffer(serverSocket, network, stickyPool, disposablePool);
            }

            public bool Push()
            {
                bool clientPushResult = Client.Push();
                bool serverPushResult = Server.Push();
                return clientPushResult || serverPushResult;
            }

            public class SocketSendBuffer
            {
                private static Bucket<byte> _stickyPool;
                private static Bucket<IDisposable> _disposablePool;

                private Socket _socket;
                private NetworkHelper _network;

                private int _bufferOffset, _disposableOffset;
                private IDisposable[] _disposableBlock;
                private byte[] _buffer;
                private bool _usePool;
                


                public SocketSendBuffer(Socket socket, NetworkHelper network, Bucket<byte> stickyPool, Bucket<IDisposable> disposablePool)
                {
                    _network = network;
                    _socket = socket;
                    _stickyPool = stickyPool;
                    _disposablePool = disposablePool;
                    _usePool = stickyPool is not null && disposablePool is not null;
                    Reset();
                }

                public void Add(Memory<byte> memory, IDisposable disposable)
                {
                    if (memory.Length > _buffer.Length)
                        throw new ArgumentOutOfRangeException(nameof(memory));

                    //如果已经积累了16个以上的PacketReceivedEventArgs就Push数据包 || 如果积累的数据即将大于最大缓存量取就Push数据 (memory = 数据 ；disposable = PacketReceivedEventArgs)
                    if ((_disposableOffset + 1 >= _disposableBlock.Length) || (_bufferOffset + memory.Length >= _buffer.Length))
                        Push();

                    memory.CopyTo(_buffer.AsMemory(_bufferOffset));
                    _disposableBlock[_disposableOffset++] = disposable;
                    _bufferOffset += memory.Length;
                }
    
                public bool Push()
                {
                    if (_bufferOffset == 0)
                        return false;

                    IDisposable[] disposableBlock = _disposableBlock;
                    byte[] buffer = _buffer;
                    int bufferLength = _bufferOffset, disposableBlocklength = _disposableOffset;
                    Reset();
                    _network.Enqueue(_socket, buffer.AsMemory(0, bufferLength), _usePool ? () =>
                    {
                        //直接在Add后Dispose会造成NetworkListener中的缓存区线程不安全，但我找了好久也没找到问题在哪里，所以暂时只能先这样延后到Push时再Dispose
                        for (int i = 0; i < disposableBlocklength; i++)
                        {
                            disposableBlock[i]?.Dispose();
                        }
                        _disposablePool.Return(disposableBlock);
                        _stickyPool.Return(buffer);
                    } : null);
                    return true;
                }

                public void Reset()
                {
                    _bufferOffset = 0;
                    _disposableOffset = 0;
                    _buffer = (_usePool ? _stickyPool.Rent() : new byte[25565]) ?? new byte[25565];
                    _disposableBlock = (_usePool ? _disposablePool.Rent() : new IDisposable[16]) ?? new IDisposable[16];
                }
            }
        }
    }
}
