using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MinecraftProtocol.IO;
using MinecraftProtocol.Utils;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.API.Event;
using NyaProxy.Extension;
using System.Collections.Concurrent;
using NyaProxy.Debug;
using NyaProxy.Configs;
using NyaProxy.Plugin;
using NyaProxy.Bridges;
using NyaProxy.Channles;
using System.Linq;
using MinecraftProtocol.IO.Pools;

namespace NyaProxy
{

    public static partial class NyaProxy
    {
        public static CommandManager CommandManager { get; private set; }
        public static ChannleContainer Channles { get; private set; }
        public static PluginManager Plugins { get; private set; }
        public static Dictionary<string, Host> Hosts { get; private set; }
        public static ConcurrentDictionary<long, Bridge> Bridges { get; private set; }
        
        public static IReadOnlyList<Socket> ServerSockets { get; set; }
        public static INyaLogger Logger { get; private set; }
        public static NetworkHelper Network;
        public static readonly CancellationTokenSource GlobalQueueToken = new CancellationTokenSource();
        public static readonly MainConfig Config = new MainConfig();

        public static EventContainer<IConnectEventArgs>      Connecting              = new();
        public static EventContainer<IHandshakeEventArgs>    Handshaking             = new();
        public static EventContainer<ILoginStartEventArgs>   LoginStart              = new();
        public static EventContainer<ILoginSuccessEventArgs> LoginSuccess            = new();
        public static EventContainer<IPacketSendEventArgs>   PacketSendToClient      = new();
        public static EventContainer<IPacketSendEventArgs>   PacketSendToServer      = new();
        public static EventContainer<IChatSendEventArgs>     ChatMessageSendToClient = new();
        public static EventContainer<IChatSendEventArgs>     ChatMessageSendToServer = new();
        public static EventContainer<IDisconnectEventArgs>   Disconnected            = new();

        public static async Task Setup(INyaLogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Hosts = new();
            Bridges = new();
            Plugins = new(logger);
            Channles = new();
            CommandManager = new();


            Thread.CurrentThread.Name = nameof(NyaProxy);
            TaskScheduler.UnobservedTaskException += (sender, e) => Crash.Report(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => Crash.Report(e.ExceptionObject as Exception);
            ReloadConfig();
            ReloadHosts();

            Network = new NetworkHelper(Config.EnableBlockingQueue, GlobalQueueToken.Token);
            if (Config.EnableReceivePool)
                NetworkListener.SetPoolSize(Config.ReceivePoolBufferLength, Config.NumberOfReceivePoolBuffers);
            if (Config.EnableStickyPool)
                QueueBridge.StickyPool = new Bucket<byte>(Config.StickyPoolBufferLength, Config.NumberOfStickyPoolBuffers, 233, true);

            QueueBridge.Setup(Config.NetworkThread);

            if (!Directory.Exists("Plugins"))
                Directory.CreateDirectory("Plugins");
            else
                foreach (var dir in Directory.GetDirectories("Plugins"))
                {
                    try
                    {
                        await Plugins.LoadAsync(dir);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        logger.Exception(e);
#else
                    //logger.Error(i18n.Plugin.Load_Error.Replace("{File}", file.Name));
#endif
                    }
                }
        }

        public static void ReloadConfig()
        {
            const string fileName = "config.toml";
            //这边不用try，如果主配置文件读取失败那么程序也不该继续运行了
            if (!File.Exists(fileName))
            {
                TomlConfigWriter writer = new TomlConfigWriter();
                Config.SetDefault();
                Config.Write(writer);
                writer.Save(fileName);
            }
            else
            {
                TomlConfigReader reader = new TomlConfigReader(fileName);
                Config.Read(reader);
            }
        }

        public static void ReloadHosts()
        {
            const string directory = "Servers";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                TomlConfigWriter writer = new TomlConfigWriter();
                HostConfig exampleConfig = new HostConfig("example");
                exampleConfig.SetDefault();
                exampleConfig.Write(writer);
                writer.Save(Path.Combine(directory, "example.toml"));
                return;
            }

            HashSet<string> removeList = new HashSet<string>(Hosts.Values.Select(x => x.Name));
            foreach (var file in Directory.GetFiles(directory).Select(f=>new FileInfo(f)))
            {
                if (file.Name == "example.toml")
                    continue;

                try
                {
                    TomlConfigReader reader = new TomlConfigReader(file.FullName);
                    Host hostConfig = new Host(file.Name.Split('.')[0]);
                    hostConfig.Read(reader);

                    if (Hosts.ContainsKey(hostConfig.Name))
                        Hosts[hostConfig.Name] =hostConfig;
                    else
                        Hosts.Add(hostConfig.Name, hostConfig);

                    removeList.Remove(hostConfig.Name);
                }
                catch (Exception e)
                {
                    Logger.Error(i18n.Error.LoadConfigFailed.Replace("{File}", file.Name));
                    Logger.Exception(e);
                }
            }
            //移除已经不存在的服务器(Bridges不需要在这边移除，Hosts内已经不存在的情况下，在连接清空后会由Bridge.Break移除)
            foreach (var name in removeList)
            {
                Hosts.Remove(name);
            }
        }

        public static void RebindSockets()
        {
            if (ServerSockets != null && ServerSockets.Count > 0)
            {
                //如果一个ServerSocket已从Bind中消失，那么就关闭该Socket。
                foreach (var socket in ServerSockets.Where(s => !Config.Bind.Any(b => s.LocalEndPoint is IPEndPoint local && local.Equals(b))))
                {
                    try
                    {
                        socket?.Close();
                    }
                    catch (Exception e)
                    {
                        if (e.CheckException<SocketException>(out string message))
                            Logger.Error(message);
                        else
                            throw;
                    }
                }
            }
            BindSockets();
        }

        public static void BindSockets()
        {
            List<Socket> serverSockets = new List<Socket>();
            foreach (var bind in Config.Bind)
            {
                //如果该地址已被绑定就跳过
                if (ServerSockets != null && ServerSockets.Any(s => s.LocalEndPoint is IPEndPoint local && local.Equals(bind)))
                    continue;

                Socket socket = BindSocket(bind);
                if (socket != null)
                    serverSockets.Add(socket);
            }

            ServerSockets = serverSockets.AsReadOnly();
        }
        

        private static Socket BindSocket(IPEndPoint bind)
        {
            try
            {
                Socket ServerSocket = new Socket(bind.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (Config.TcpFastOpen && OperatingSystem.IsLinux())
                    ServerSocket.EnableLinuxFastOpenServer();

                ServerSocket.Bind(bind);
                ServerSocket.Listen();
                SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
                eventArgs.UserToken = ServerSocket;
                eventArgs.Completed += (sender, e) => AcceptCompleted(e);
                Logger.Info(i18n.Message.ListenON.Replace("{Bind}", bind));
                if (!ServerSocket.AcceptAsync(eventArgs))
                    AcceptCompleted(eventArgs);
                return ServerSocket;
            }
            catch (Exception e)
            {
                if (e.CheckException<SocketException>(out string message))
                    Logger.Error(message);
                else
                    throw;
                return null;
            }
        }

        private static void AcceptCompleted(SocketAsyncEventArgs e)
        {
            try
            {

                ConnectEventArgs eventArgs = new ConnectEventArgs(Bridge.GetNextSessionId(), e.AcceptSocket, e.SocketError);
                Connecting.Invoke(e.AcceptSocket, eventArgs, Logger);
                if (!eventArgs.IsBlock)
                {
                    Socket AcceptSocket = eventArgs.AcceptSocket;
                    switch (e.SocketError)
                    {
                        case SocketError.Success: SessionHanderAsync(eventArgs.SessionId, AcceptSocket); break;
                        default: AcceptSocket.Close(); break;
                    }
                }

                e.AcceptSocket = null;

                if (!(e.UserToken as Socket).AcceptAsync(e))
                    AcceptCompleted(e);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { Logger.Exception(ex); }
        }

        private static async Task SessionHanderAsync(long sessionId, Socket acceptSocket)
        {
            HandshakeEventArgs hea = null;
            try
            {
                using Packet FirstPacket = await ProtocolUtils.ReceivePacketAsync(acceptSocket);
                if (HandshakePacket.TryRead(FirstPacket, -1, out HandshakePacket hp))
                {

                    Host destHost = GetHost(hp.GetServerAddressOnly(), hp.ServerPort);
                    hea = new HandshakeEventArgs(sessionId, acceptSocket, hp, destHost);
                    Handshaking.Invoke(acceptSocket, hea, Logger);
                    if (hea.IsBlock)
                    {
                        acceptSocket.Close();
                        return;
                    }

                    destHost = GetHost(hea.Packet.GetServerAddressOnly(), hea.Packet.ServerPort);
                    if (destHost.ForwardMode == ForwardMode.Dorp)
                        return;
                    if (destHost.ForwardMode == ForwardMode.Reject)
                        acceptSocket.Close();


                    Socket serverSocket = await destHost.OpenConnectAsync();
                    if (!NetworkUtils.CheckConnect(serverSocket))
                        throw new DisconnectException(i18n.Disconnect.ConnectFailed);

                    if (hp.NextState == HandshakeState.GetStatus)
                        Logger.Info($"{acceptSocket._remoteEndPoint()} Try to ping {destHost.Name}[{serverSocket._remoteEndPoint()}]");

                    

                    if (destHost.ForwardMode == ForwardMode.Direct)
                    {
                        Network.Enqueue(serverSocket, hp.Pack(-1), () =>
                        {
                            FastBridge fastBridge = new FastBridge(hea.SessionId, destHost, hea.Packet.ServerAddress, acceptSocket, serverSocket);
                            fastBridge.Build();
                            hea?.Packet?.Dispose();
                        });
                    }
                    else
                    {
                        QueueBridge queueBridge = new QueueBridge(hea.SessionId, destHost, hea.Packet, acceptSocket, serverSocket);
                        queueBridge.Build();
                    }
                }
                else
                {
                    Logger.Debug(i18n.Debug.ReceivedEnexpectedPacketDuringHandshake.Replace("{EndPoint}", acceptSocket._remoteEndPoint(), "{Packet}", FirstPacket));
                    throw new DisconnectException(i18n.Disconnect.HandshakeFailed);
                }
            }
            catch (Exception e)
            {
                if (e.CheckException<DisconnectException>(out string disconnectMessage))
                {
                    if (hea != null && hea.Packet.NextState == HandshakeState.Login)
                        acceptSocket.DisconnectOnLogin(disconnectMessage);
                    else
                        acceptSocket.Close();
                    hea?.Packet?.Dispose();
                    Logger.Debug(disconnectMessage);
                }
                else if(e.CheckException<SocketException>(out string message))
                {
                    if (hea != null && hea.Packet.NextState == HandshakeState.Login)
                        acceptSocket.DisconnectOnLogin(i18n.Disconnect.ServerClosed);
                    Logger.Debug($"SocketException {message}");
                }
                else
                {
                    Logger.Exception(e);
                }
            }
        }

        public static Host GetHost(string hostName, ushort hostPort)
        {
            Host host;
            if (Hosts.TryGetValue($"{hostName}:{hostPort}", out host))
                return host;
            else if (Hosts.TryGetValue(hostName, out host))
                return host;
            else if (Hosts.TryGetValue("default", out host))
                return host;
            else
                throw new DisconnectException(i18n.Disconnect.NoServerAvailable);
        }
    }
}
