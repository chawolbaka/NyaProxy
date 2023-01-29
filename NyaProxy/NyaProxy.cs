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

namespace NyaProxy
{
    public static partial class NyaProxy
    {
        public static CommandManager CommandManager { get; private set; }
        public static ChannleContainer Channles { get; private set; }
        public static PluginManager Plugin { get; private set; }
        public static ConcurrentDictionary<string, ConcurrentDictionary<long, Bridge>> Bridges { get; private set; }
        public static Config Config { get; private set; }
        public static ILogger Logger { get; private set; }

        public static EventHandler<IConnectEventArgs> Connecting;
        public static EventHandler<IHandshakeEventArgs> Handshaking;
        public static EventHandler<ILoginStartEventArgs> LoginStart;
        public static EventHandler<ILoginSuccessEventArgs> LoginSuccess;
        public static EventHandler<IPacketSendEventArgs> PacketSendToClient;
        public static EventHandler<IPacketSendEventArgs> PacketSendToServer;
        public static EventHandler<IChatSendEventArgs> ChatMessageSendToClient;
        public static EventHandler<IChatSendEventArgs> ChatMessageSendToServer;
        public static AsyncCommonEventHandler<object, IAsyncChatEventArgs> ChatMessageSened;
        public static EventHandler<IDisconnectEventArgs> Disconnected;

        public static async Task Setup(Config config, ILogger logger)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Plugin = new(logger);
            Bridges = new ();
            Channles = new();
            CommandManager = new();

            Thread.CurrentThread.Name = $"{nameof(NyaProxy)} thread";
           
            TaskScheduler.UnobservedTaskException += (sender, e) => Crash.Report(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => Crash.Report(e.ExceptionObject as Exception);
            if (!config.IsLoad)
                config.Load();

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
            BlockingBridge.Setup(config.NetworkThread);
        }

        public static void Start()
        {
            Socket ServerSocket = new Socket(Config.Bind.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(Config.Bind);
            ServerSocket.Listen();
            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            eventArgs.UserToken = ServerSocket;
            eventArgs.Completed += (sender, e) => AcceptCompleted(e);
            Logger.Info(i18n.Message.ListenON.Replace("{Bind}", Config.Bind));
            if (!ServerSocket.AcceptAsync(eventArgs))
                AcceptCompleted(eventArgs);
            
        }

        private static void AcceptCompleted(SocketAsyncEventArgs e)
        {
            ConnectEventArgs eventArgs = new ConnectEventArgs(e.AcceptSocket, e.SocketError);
            EventUtils.InvokeCancelEvent(Connecting, e.AcceptSocket, eventArgs);
            if(!eventArgs.IsBlock)
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

                         Logger.Debug($"{acceptSocket._remoteEndPoint} Try to handshake (Host = {hea.Packet.ServerAddress}, Port = {hea.Packet.ServerPort})");

                         string rawHandshakeAddress = hea.Packet.ServerAddress;
                         string host = hea.Packet.ServerAddress;
                         if (host.Contains('\0'))
                             host = host.Substring(0, host.IndexOf('\0'));

                         HostConfig dest = ChooseServer(host);
                         Socket serverSocket = await dest?.OpenConnectAsync();
                         
                         
                         if (dest == null)
                         {
                             acceptSocket.DisconnectOnLogin(i18n.Disconnect.NoServerAvailable);
                             acceptSocket.Close();
                             return;
                         }
                         else if (!NetworkUtils.CheckConnect(serverSocket))
                         {
                             acceptSocket.DisconnectOnLogin(i18n.Disconnect.ConnectFailed);
                             acceptSocket.Close();
                             return;
                         }

                         if (hea.Packet.NextState == HandshakePacket.State.GetStatus)
                         {
                             await NetworkUtils.SendDataAsync(serverSocket, hp.Pack(-1));
                             hp?.Dispose();
                             FastBridge bridge = new FastBridge(dest, rawHandshakeAddress, acceptSocket, serverSocket);
                             bridge.Build();
                         }
                         else if (hea.Packet.NextState == HandshakePacket.State.Login)
                         {
                             Packet lsPacket = ProtocolUtils.ReceivePacket(acceptSocket);
                             if (LoginStartPacket.TryRead(lsPacket, -1, out LoginStartPacket lsp))
                             {
                                 LoginStartEventArgs lsea = new LoginStartEventArgs(lsp);
                                 EventUtils.InvokeCancelEvent(LoginStart, acceptSocket, lsea);
                                 if (lsea.IsBlock)
                                     return;

                                 Logger.Info(i18n.Message.ConnectCreated.Replace("{PlayerName}", lsea.Packet.PlayerName, "{Souce.EndPoint}", acceptSocket.RemoteEndPoint, "{Destination.EndPoint}", serverSocket.RemoteEndPoint));

                                 switch (dest.ForwardMode)
                                 {
                                     case ForwardMode.Direct:
                                         if (dest.Flags.HasFlag(ServerFlags.Forge) && !hea.Packet.ServerAddress.Contains("\0FML\0"))
                                             hea.Packet.ServerAddress += "\0FML\0";
                                         break;
                                     case ForwardMode.NyaProxy:
                                         break;
                                     case ForwardMode.BungeeCord:
                                         hea.Packet.ServerAddress = new StringBuilder(host)
                                             .Append($"\0{(acceptSocket._remoteEndPoint() as IPEndPoint).Address}")
                                             .Append($"\0{(dest.Flags.HasFlag(ServerFlags.OnlineMode) ? await UUID.GetFromMojangAsync(lsea.Packet.PlayerName) : UUID.GetFromPlayerName(lsea.Packet.PlayerName))}\0")
                                             .Append(dest.Flags.HasFlag(ServerFlags.Forge) ? "FML\0" : "").ToString();
                                         break;
                                     case ForwardMode.BungeeGuard:
                                         break;
                                     case ForwardMode.Modern:
                                         break;
                                     default:
                                         break;
                                 }

                                 if (!dest.Flags.HasFlag(ServerFlags.OnlineMode))
                                 {
                                     BlockingBridge bb = new BlockingBridge(dest, rawHandshakeAddress, acceptSocket, serverSocket, hea.Packet.ProtocolVersion);
                                     bb.Build(hea.Packet, lsea.Packet);
                                 }
                                 else
                                 {
                                     acceptSocket.DisconnectOnLogin("无法支持正版登录");
                                     hp?.Dispose();
                                     lsea?.Packet?.Dispose();
                                 }
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
                     if (e.CheckSocketException(out string message))
                         Logger.Error(message);
                     else
                         Logger.Exception(e);
                 }
             });
        }

        public static HostConfig ChooseServer(string host)
        {
            if (Config.Hosts.ContainsKey(host))
                return Config.Hosts[host];
            else if (Config.Hosts.ContainsKey("default"))
                return  Config.Hosts["default"];
            else
                return null;
        }
    }
}
