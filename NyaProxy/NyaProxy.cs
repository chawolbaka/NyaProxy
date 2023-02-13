using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MinecraftProtocol.Utils;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Extension;
using System.Collections.Concurrent;
using NyaProxy.Debug;
using NyaProxy.Configs;
using NyaProxy.Plugin;
using NyaProxy.Bridges;
using NyaProxy.Channles;
using System.Linq;
using MinecraftProtocol.IO;
using MinecraftProtocol.Compatible;
using System.Xml.Linq;

namespace NyaProxy
{
    public static partial class NyaProxy
    {
        public static CommandManager CommandManager { get; private set; }
        public static ChannleContainer Channles { get; private set; }
        public static PluginManager Plugin { get; private set; }
        public static ConcurrentDictionary<string, ConcurrentDictionary<long, Bridge>> Bridges { get; private set; }
        public static IReadOnlyList<Socket> ServerSockets { get; set; }
        
        
        public static ILogger Logger { get; private set; }

        public static readonly MainConfig Config = new MainConfig();
        public static readonly Dictionary<string, HostConfig> Hosts = new Dictionary<string, HostConfig>();
        

        public static EventHandler<IConnectEventArgs> Connecting;
        public static EventHandler<IHandshakeEventArgs> Handshaking;
        public static EventHandler<ILoginStartEventArgs> LoginStart;
        public static EventHandler<ILoginSuccessEventArgs> LoginSuccess;
        public static EventHandler<IPacketSendEventArgs> PacketSendToClient;
        public static EventHandler<IPacketSendEventArgs> PacketSendToServer;
        public static EventHandler<IChatSendEventArgs> ChatMessageSendToClient;
        public static EventHandler<IChatSendEventArgs> ChatMessageSendToServer;
        public static EventHandler<IDisconnectEventArgs> Disconnected;

        public static async Task Setup(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Plugin = new(logger);
            Bridges = new ();
            Channles = new();
            CommandManager = new();

            Thread.CurrentThread.Name = $"{nameof(NyaProxy)} thread";
            TaskScheduler.UnobservedTaskException += (sender, e) => Crash.Report(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => Crash.Report(e.ExceptionObject as Exception);
            ReloadConfig();
            ReloadHosts();

            if (Config.EnableReceivePool)
                NetworkListener.SetPoolSize(Config.ReceivePoolBufferLength, Config.ReceivePoolBufferCount);

            BlockingBridge.Setup(Config.NetworkThread);

            if (!Directory.Exists("Plugins"))
                Directory.CreateDirectory("Plugins");
            else
                foreach (var dir in Directory.GetDirectories("Plugins"))
                {
                    try
                    {
                        await Plugin.LoadAsync(dir);
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
                    HostConfig hostConfig = new HostConfig(file.Name.Split('.')[0]);
                    hostConfig.Read(reader);

                    if (Hosts.ContainsKey(hostConfig.Name))
                        Hosts[hostConfig.Name] = hostConfig;
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
            //移除已经不存在的服务器
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
                    break;

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
                ConnectEventArgs eventArgs = new ConnectEventArgs(e.AcceptSocket, e.SocketError);
                EventUtils.InvokeCancelEvent(Connecting, e.AcceptSocket, eventArgs);
                if (!eventArgs.IsBlock)
                {
                    Socket AcceptSocket = eventArgs.AcceptSocket;
                    switch (e.SocketError)
                    {
                        case SocketError.Success: SessionHanderAsync(AcceptSocket); break;
                        default: AcceptSocket.Close(); break;
                    }
                }

                e.AcceptSocket = null;

                if (!(e.UserToken as Socket).AcceptAsync(e))
                    AcceptCompleted(e);
            }
            catch (ObjectDisposedException) { }
        }

        private static Task SessionHanderAsync(Socket acceptSocket)
        {
            return Task.Run(async () =>
             {
                 try
                 {
                     using Packet FirstPacket = ProtocolUtils.ReceivePacket(acceptSocket);
                     if (HandshakePacket.TryRead(FirstPacket, -1, out HandshakePacket hp))
                     {
                         HandshakeEventArgs hea = new HandshakeEventArgs(hp);
                         EventUtils.InvokeCancelEvent(Handshaking, acceptSocket, hea);
                         if (hea.IsBlock)
                             return;

                         Logger.Debug($"{acceptSocket._remoteEndPoint()} Try to handshake (Host = {hea.Packet.ServerAddress}, Port = {hea.Packet.ServerPort})");

                        
                         string rawHandshakeAddress = hea.Packet.ServerAddress;
                         string host = hea.Packet.ServerAddress;
                         if (host.Contains('\0'))
                             host = host.Substring(0, host.IndexOf('\0'));

                         HostConfig dest = ChooseServer(host);
                         if (dest == null)
                         {
                             acceptSocket.DisconnectOnLogin(i18n.Disconnect.NoServerAvailable);
                             return;
                         }

                         int ProtocolVersion = dest.ProtocolVersion > 0 ? dest.ProtocolVersion : hea.Packet.ProtocolVersion;
                         Socket serverSocket = await dest.OpenConnectAsync();
                         if (!NetworkUtils.CheckConnect(serverSocket))
                         {
                             acceptSocket.DisconnectOnLogin(i18n.Disconnect.ConnectFailed);
                             return;
                         }
                         
              

                         if (dest.ForwardMode == ForwardMode.Direct || hea.Packet.NextState == HandshakePacket.State.GetStatus)
                         {
                             BlockingBridge.Enqueue(serverSocket, hp.Pack(-1), () => 
                             {
                                 FastBridge bridge = new FastBridge(dest, rawHandshakeAddress, acceptSocket, serverSocket);
                                 bridge.Build();
                                 hp?.Dispose();
                             });                             
                         }
                         else if (hea.Packet.NextState == HandshakePacket.State.Login)
                         {
                             using Packet SecondPacket = ProtocolUtils.ReceivePacket(acceptSocket);
                             if (LoginStartPacket.TryRead(SecondPacket, -1, out LoginStartPacket lsp))
                             {
                                 LoginStartEventArgs lsea = new LoginStartEventArgs(lsp);
                                 EventUtils.InvokeCancelEvent(LoginStart, acceptSocket, lsea);
                                 if (lsea.IsBlock)
                                     return;

                                
                                 string forgeTag = ProtocolVersion >= ProtocolVersions.V1_13 ? "\0FML2\0" : "\0FML\0"; ///好像Forge是在1.13更换了协议的?
                                 switch (dest.ForwardMode)
                                 {
                                     case ForwardMode.Default:
                                         if (dest.Flags.HasFlag(ServerFlags.Forge) && !hea.Packet.ServerAddress.Contains(forgeTag))
                                             hea.Packet.ServerAddress += forgeTag;
                                         break;
                                     case ForwardMode.BungeeCord:
                                         hea.Packet.ServerAddress = new StringBuilder(host)
                                             .Append($"\0{(acceptSocket._remoteEndPoint() as IPEndPoint).Address}")
                                             .Append($"\0{(dest.Flags.HasFlag(ServerFlags.OnlineMode) ? await UUID.GetFromMojangAsync(lsea.Packet.PlayerName) : UUID.GetFromPlayerName(lsea.Packet.PlayerName))}")
                                             .Append(dest.Flags.HasFlag(ServerFlags.Forge) ? forgeTag : "\0").ToString();
                                         break;
                                     default:
                                         throw new NotImplementedException();
                                 }

                                 BlockingBridge bb = new BlockingBridge(dest, rawHandshakeAddress, acceptSocket, serverSocket, ProtocolVersion);
                                 bb.Build(hea.Packet, lsea.Packet);
                                 Logger.Info(i18n.Message.ConnectCreated.Replace("{PlayerName}", lsea.Packet.PlayerName, "{Souce.EndPoint}", acceptSocket.RemoteEndPoint, "{Destination.EndPoint}", serverSocket.RemoteEndPoint));
                             }
                         }
                         else
                         {
                             acceptSocket.DisconnectOnLogin(i18n.Disconnect.HandshakeFailed);
                         }
                     }
                     else
                     {
                         Logger.Debug("异常的握手包");
                         acceptSocket.Close();
                     }
                 }
                 catch (Exception e)
                 {
                     if (e.CheckException<SocketException>(out string message))
                         Logger.Error(message);
                     else if (e.CheckException<DisconnectException>(out string disconnectMessage))
                         acceptSocket.DisconnectOnLogin(disconnectMessage);
                     else
                         Logger.Exception(e);
                 }
             });
        }

        public static HostConfig ChooseServer(string host)
        {
            if (Hosts.ContainsKey(host))
                return Hosts[host];
            else if (Hosts.ContainsKey("default"))
                return  Hosts["default"];
            else
                return null;
        }
    }
}
