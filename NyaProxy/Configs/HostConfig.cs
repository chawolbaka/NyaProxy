using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Configs.Rule;
using NyaProxy.Extension;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Utils;

namespace NyaProxy.Configs
{
    public class HostConfig : Config, IDefaultConfig, IManualConfig, IHostConfig, IHostTargetRule
    {
        public string Name { get; set; }
        public ServerSelectMode SelectMode { get; set; }
        public ForwardMode ForwardMode { get; set; }
        public List<EndPoint> ServerEndPoints { get; set; }
        public int CompressionThreshold { get; set; }
        public int ProtocolVersion { get; set; }
        public ServerFlags Flags { get; set; }
        public bool TcpFastOpen { get; set; }
        public int ConnectionTimeout { get; set; }
        public int ConnectionThrottle { get; set; }

        public Dictionary<string, HostTargetRule> PlayerRules { get; set; }

        private static Random Random = new Random();
        private SafeIndex _serverIndex;
        private DateTime _lastConnectTime = default;

        public HostConfig(string uniqueId) : base(uniqueId)
        {

        }

        public void Read(ConfigReader reader)
        {
            Name                 = reader.ReadStringProperty("host");
            ForwardMode          = Enum.Parse<ForwardMode>(reader.ReadStringProperty("forwarding-mode"));
            Flags                = reader.ContainsKey("server-flags") ? Enum.Parse<ServerFlags>(reader.ReadStringProperty("server-flags")) : ServerFlags.None;
            SelectMode           = reader.ContainsKey("server-select-mode") ? Enum.Parse<ServerSelectMode>(reader.ReadStringProperty("server-select-mode")) : ServerSelectMode.Failover;
            ProtocolVersion      = reader.ContainsKey("server-version") ? ReadProtocolVersionByConfigNode(reader.ReadProperty("server-version")) : -1;
            CompressionThreshold = reader.ContainsKey("compression-threshold") ? (int)reader.ReadNumberProperty("compression-threshold") : -1;
            TcpFastOpen          = reader.ContainsKey("tcp-fast-open") ? reader.ReadBooleanProperty("tcp-fast-open") : false;
            ConnectionTimeout    = reader.ContainsKey("connection-timeout") ? (int)reader.ReadNumberProperty("connection-timeout")   : -1;
            ConnectionThrottle   = reader.ContainsKey("connection-throttle") ? (int)reader.ReadNumberProperty("connection-throttle") : -1;


            List<EndPoint> serverEndPoints = new List<EndPoint>();
            foreach (StringNode server in reader.ReadArrayProperty("servers"))
            {
                if (IPEndPoint.TryParse(server, out IPEndPoint ipEndPoint))
                {
                    serverEndPoints.Add(ipEndPoint);
                }
                else
                {
                    string[] s = server.Value.Split(':');
                    if (s.Length == 2 && ushort.TryParse(s[1], out ushort port))
                    {
                        serverEndPoints.Add(new DnsEndPoint(s[0], port));
                    }
                }
            }
            Dictionary<string, HostTargetRule> playerRules = new Dictionary<string, HostTargetRule>();
            if (reader.ContainsKey("rule"))
            {
                foreach (var rule in reader.ReadArrayProperty("rule").Select(a => (a as ObjectNode).Nodes))
                {
                    HostTargetRule targetRule = new HostTargetRule(Enum.Parse<TargetType>((string)rule["target-type"]), (string)rule["target"]);
                    targetRule.Flags                = reader.ContainsKey("server-flags") ? Enum.Parse<ServerFlags>((string)rule["server-flags"]) : Flags;
                    targetRule.ProtocolVersion      = reader.ContainsKey("server-version") ? ReadProtocolVersionByConfigNode(rule["server-version"]) : ProtocolVersion;
                    targetRule.CompressionThreshold = reader.ContainsKey("compression-threshold") ? (int)rule["compression-threshold"] : CompressionThreshold;
                    playerRules.Add(targetRule.Target, targetRule);
                }
            }

            ServerEndPoints = serverEndPoints;
            PlayerRules = playerRules;

            _serverIndex = new SafeIndex(serverEndPoints.Count);
            int ReadProtocolVersionByConfigNode(ConfigNode node)
            {
                if (node is StringNode pvsn)
                    return ProtocolVersions.SearchByName(pvsn);
                else
                    return (int)node;
            }
        }

        public void Write(ConfigWriter writer)
        {
            try
            {
                writer.WriteProperty("host", new StringNode(Name));

                ArrayNode servers = new ArrayNode();

                for (int i = 0; i < ServerEndPoints.Count; i++)
                {
                    if (ServerEndPoints[i] is DnsEndPoint dnsEndPoint)
                        servers.Value.Add(new StringNode($"{dnsEndPoint.Host}:{dnsEndPoint.Port}"));
                    else
                        servers.Value.Add(new StringNode(ServerEndPoints[i].ToString()));
                }
                writer.WriteProperty("servers", servers);

                writer.WriteProperty("server-select-mode",          new StringNode(SelectMode.ToString(), i18n.Config.SelectMode));
                writer.WriteProperty("server-flags",                new StringNode(Flags.ToString(), i18n.Config.ServerFlags));
                writer.WriteProperty("server-version",              new NumberNode(ProtocolVersion, i18n.Config.ProtocolVersion));
                writer.WriteProperty("compression-threshold",       new NumberNode(CompressionThreshold, i18n.Config.CompressionThreshold));
                writer.WriteProperty("player-info-forwarding-mode", new StringNode(ForwardMode.ToString(), i18n.Config.ForwardMode));
                writer.WriteProperty("tcp-fast-open",               new BooleanNode(TcpFastOpen, i18n.Config.ClientTcpFastOpen));
                writer.WriteProperty("connection-timeout",          new NumberNode(ConnectionTimeout, i18n.Config.ConnectionTimeout));
                writer.WriteProperty("connection-throttle",         new NumberNode(ConnectionThrottle, i18n.Config.ConnectionThrottle));

                if (PlayerRules.Count > 0)
                {
                    ArrayNode rules = new ArrayNode();

                    foreach (var rule in PlayerRules.Values)
                    {
                        rules.Add(new ObjectNode(new Dictionary<string, ConfigNode>
                        {
                            ["target-type"]           = new StringNode(rule.Type.ToString()),
                            ["target"]                = new StringNode(rule.Target),
                            ["server-flags"]          = new StringNode(rule.Flags.ToString()),
                            ["server-version"]        = new NumberNode(rule.ProtocolVersion),
                            ["compression-threshold"] = new NumberNode(rule.CompressionThreshold)
                        }));
                    }
                    writer.WriteProperty("rule", rules);
                }
            }
            catch (Exception e)
            {
                NyaProxy.Logger.Error(i18n.Error.SaveConfigFailed.Replace("{File}", $"{UniqueId}.{writer.FileType}"));
                NyaProxy.Logger.Exception(e);
            }
        }

        public void SetDefault()
        {
            Name = "example";
            ForwardMode = ForwardMode.Default;
            SelectMode = ServerSelectMode.Failover;
            Flags = ServerFlags.None;
            ServerEndPoints = new List<EndPoint>() { new DnsEndPoint("example.net", 25565), new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 233) };
            ProtocolVersion = -1;
            CompressionThreshold = -1;
            TcpFastOpen = false;
            ConnectionTimeout = 1000 * 6;
            ConnectionThrottle = 2000;
        }

        public IHostTargetRule GetRule(string name)
        {
            if (PlayerRules.ContainsKey(name))
                return PlayerRules[name];
            else
                return this;
        }


        public async Task<Socket> OpenConnectAsync(EndPoint endPoint)
        {
            if (ConnectionThrottle > 0 && _lastConnectTime != default && (DateTime.Now - _lastConnectTime).TotalMilliseconds < ConnectionThrottle)
                throw new DisconnectException("连接频率过高");

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); ;
            if (TcpFastOpen)
            {
                if (OperatingSystem.IsWindows())
                    socket.EnableWindowsFastOpenClient();
                else if (OperatingSystem.IsLinux())
                    socket.EnableLinuxFastOpenConnect();
            }

            await socket.ConnectAsync(endPoint);
            _lastConnectTime = DateTime.Now;
            if (!NetworkUtils.CheckConnect(socket))
                throw new DisconnectException(i18n.Disconnect.ConnectFailed);

            return socket;
        }
        public async Task<Socket> OpenConnectAsync(EndPoint endPoint, int timeout)
        {
            if (timeout < 0)
                return await OpenConnectAsync(endPoint);

            Task<Socket> connectTask = OpenConnectAsync(endPoint);
            Task timeoutTask = Task.Delay(timeout);
            await Task.WhenAny(connectTask, timeoutTask);
            if (timeoutTask.IsCompletedSuccessfully)
            {

#pragma warning disable CS4014 // 这边需要在connectTask完成后关闭连接，但异常需要立刻抛出而不是connectTask完成后再抛出，所以不需要等待。
                timeoutTask.ContinueWith(async (_) => 
                {
                    try { 
                        Socket socket = await connectTask; 
                        if (socket.Connected) 
                            socket.Close(); 
                    }
                    catch (Exception) {  }
                });
                throw new SocketException((int)SocketError.TimedOut);
            }
            else
            {
                return await connectTask;
            }
        }

        public async Task<Socket> OpenConnectAsync()
        {
            if (SelectMode == ServerSelectMode.Failover)
            {
                var servers = ServerEndPoints.ToArray();
                for (int i = 0; i < servers.Length; i++)
                {
                    try
                    {
                        return await OpenConnectAsync(servers[i], ConnectionTimeout);
                    }
                    catch (SocketException e)
                    {
                        if (i + 1 >= servers.Length)
                            throw new Exception(i18n.Error.NoSocketAvailable, e);
                    }
                }
            }
            else if (SelectMode == ServerSelectMode.Pool)
            {
                return await OpenConnectAsync(ServerEndPoints[_serverIndex.Get()], ConnectionTimeout);
            }
            else if (SelectMode == ServerSelectMode.Random)
            {
                return await OpenConnectAsync(ServerEndPoints[Random.Next(0, ServerEndPoints.Count - 1)], ConnectionTimeout);
            }

            throw new Exception(i18n.Error.NoSocketAvailable);
        }


        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
