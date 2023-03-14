using System.Net;
using System.Text.Json;
using NyaProxy.API;
using NyaProxy.API.Enum;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.IO.Extensions;
using NyaProxy.API.Event;
using MinecraftProtocol.Utils;

namespace Motd
{
    public class MotdPlugin : NyaPlugin
    {
        public static MotdPlugin CurrentInstance;
        public string HostsPath;

        public Dictionary<string, int> HostIndex = new Dictionary<string, int>();
        public Dictionary<EndPoint, int> CurrentConnections = new Dictionary<EndPoint, int>();

        public override async Task OnEnable()
        {
            CurrentInstance = this;
            HostsPath = Path.Combine(Helper.WorkDirectory.FullName, "Hosts");
            Helper.Config.RegisterConfigCommand = false;
            
            if(!Directory.Exists(HostsPath))
            {
                Directory.CreateDirectory(HostsPath);
                Helper.Config.Register(typeof(MotdConfig), Path.Combine(HostsPath, $"example.{Helper.Config.DefaultFileType}"));
            }
            else
            {
                ReloadConfig();
            }
            Helper.Events.Transport.PacketSendToServer += OnPacketSendToServer;
            Helper.Events.Transport.Handshaking  += OnHandshaking;
            Helper.Events.Transport.LoginSuccess += OnLoginSuccess;
            Helper.Events.Transport.Disconnected += OnDisconnected;
            Helper.Command.Register(new ConfigCommand(ReloadConfig));
            Helper.Command.Register(new SimpleCommand("get", async (args, helper) => {
                ServerListPing slp = new ServerListPing(await NetworkUtils.GetIPEndPointAsync(args.Span[0]));
                PingReply result = await slp.SendAsync();
                helper.Logger.Unpreformat(result.Json);
            }));
        }

        [EventPriority(EventPriority.Highest)]
        private void OnHandshaking(object? sender, IHandshakeEventArgs e)
        {
            if (e.Packet.NextState == HandshakeState.Login)
                return;

            //如果存在该Host的配置文件就接管该连接
            string host = e.Packet.GetServerAddressOnly();
            if (HostIndex.ContainsKey(host) && e.Source.RemoteEndPoint is IPEndPoint source)
                CurrentConnections.Add(source, HostIndex[host]);
        }

        private void OnPacketSendToServer(object? sender, IPacketSendEventArgs e)
        {
            if (e.Stage == Stage.Login || e.Stage == Stage.Play)
                return;

            if (e.Stage == Stage.Handshake && e.Packet == PacketType.Handshake && e.Packet.AsHandshake().NextState == HandshakeState.GetStatus)
            {
                //如果该连接已被接管就阻止握手包被发送到服务器
                if (CurrentConnections.ContainsKey(e.Source.RemoteEndPoint!))
                    e.Block();
            }
            else if (e.Stage == Stage.Status && e.Packet == PacketType.Status.Client.Request && CurrentConnections.ContainsKey(e.Source.RemoteEndPoint!))
            {
                PingReply pingReply = Helper.Config.Get<MotdConfig>(CurrentConnections[e.Source.RemoteEndPoint!]).PingReply;
                string json = JsonSerializer.Serialize(pingReply);
                PingResponsePacket packet = new PingResponsePacket(json);
                Helper.Network.Enqueue(e.Source, packet.AsCompatible(e.ProtocolVersion, e.CompressionThreshold));
                CurrentConnections.Remove(e.Source.RemoteEndPoint!);
                e.Block();
            }
            else if (e.Stage == Stage.Status & e.Packet == PacketType.Status.Client.Ping)
            {
                //改个id原封不动的直接送回去
                e.Packet.Id = PongPacket.GetPacketId();
                e.Destination = e.Source;
            }
        }

        private void OnLoginSuccess(object? sender, ILoginSuccessEventArgs e)
        {
            if (HostIndex.ContainsKey(e.Host))
                Helper.Config.Get<MotdConfig>(HostIndex[e.Host]).PingReply.Player.Online++;
        }

        private void OnDisconnected(object? sender, IDisconnectEventArgs e)
        {
            if (HostIndex.ContainsKey(e.Host))
                Helper.Config.Get<MotdConfig>(HostIndex[e.Host]).PingReply.Player.Online--;
        }

        public override async Task OnDisable()
        {
            Helper.Events.Transport.Handshaking  -= OnHandshaking;
            Helper.Events.Transport.LoginSuccess -= OnLoginSuccess;
            Helper.Events.Transport.Disconnected -= OnDisconnected;
            Helper.Events.Transport.PacketSendToServer -= OnPacketSendToServer;
        }


        private void ReloadConfig()
        {
            CurrentConnections.Clear();
            Helper.Config.Clear(); HostIndex.Clear();
            foreach (var file in Directory.GetFiles(HostsPath!).Select(f=>new FileInfo(f)))
            {
                try
                {
                    int index = Helper.Config.Register(typeof(MotdConfig), file.FullName);
                    MotdConfig config = Helper.Config.Get<MotdConfig>(index);
                    HostIndex.Add(config.Host, index);
                    Logger.Info($"{file.Name} load success.");
                }
                catch (Exception e)
                {
                    Logger.Info($"{file.Name} load fail.");
                    Logger.Exception(e);
                }
            }
        }
    }
}