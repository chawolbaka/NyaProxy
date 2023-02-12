using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.API.Enum;


namespace NyaProxy.Configs
{
    public class HostConfig : Config, IDefaultConfig, IManualConfig, IHostConfig
    {
        public string Name { get; set; }
        public ServerSelectMode SelectMode { get; set; }
        public ForwardMode ForwardMode { get; set; }
        public List<EndPoint> ServerEndPoints { get; set; }
        public int CompressionThreshold { get; set; }
        public int ProtocolVersion { get; set; }
        public bool KickExistsPlayer { get; set; }
        public ServerFlags Flags { get; set; }

        private static Random Random = new Random();
        private SafeIndex ServerIndex;

        public HostConfig(string uniqueId) : base(uniqueId)
        {
        }

        public void Read(ConfigReader reader)
        {
            try
            {
                Name = reader.ReadStringProperty("host");
                ProtocolVersion = (int)reader.ReadNumberProperty("server-version");
                Flags = Enum.Parse<ServerFlags>(reader.ReadStringProperty("server-flags"));
                SelectMode = Enum.Parse<ServerSelectMode>(reader.ReadStringProperty("server-select-mode"));
                ForwardMode = Enum.Parse<ForwardMode>(reader.ReadStringProperty("player-info-forwarding-mode"));

                CompressionThreshold = reader.ContainsKey("compression-threshold") ? (int)reader.ReadNumberProperty("compression-threshold") : -1;
                KickExistsPlayer = reader.ReadBooleanProperty("kick-exists-player");

                List<EndPoint> serverEndPoints = new List<EndPoint>();
                foreach (StringNode server in reader.ReadArray("servers"))
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
                ServerIndex = new SafeIndex(serverEndPoints.Count);
                ServerEndPoints = serverEndPoints;
            }
            catch (Exception e)
            {
                NyaProxy.Logger.Error(i18n.Error.LoadConfigFailed.Replace("{File}", $"{Name}.{reader.FileType}"));
                NyaProxy.Logger.Exception(e);
            }
        }

        public void Write(ConfigWriter writer)
        {
            try
            {
                writer.WriteProperty("host", new StringNode(i18n.Config.Host));

                ConfigArray servers = new ConfigArray();

                for (int i = 0; i < ServerEndPoints.Count; i++)
                {
                    if (ServerEndPoints[i] is DnsEndPoint dnsEndPoint)
                        servers.Value.Add(new StringNode($"{dnsEndPoint.Host}:{dnsEndPoint.Port}"));
                    else
                        servers.Value.Add(new StringNode(ServerEndPoints[i].ToString()));
                }
                writer.WriteArray("servers", servers);

                writer.WriteProperty("server-select-mode",          new StringNode(SelectMode.ToString(),i18n.Config.SelectMode));
                writer.WriteProperty("server-version",              new NumberNode(ProtocolVersion, i18n.Config.ProtocolVersion));
                writer.WriteProperty("server-flags",                new StringNode(Flags.ToString(), i18n.Config.ServerFlags));
                writer.WriteProperty("compression-threshold",       new NumberNode(CompressionThreshold, i18n.Config.CompressionThreshold));
                writer.WriteProperty("player-info-forwarding-mode", new StringNode(ForwardMode.ToString(), i18n.Config.ForwardMode));
                writer.WriteProperty("kick-exists-player",          new BooleanNode(KickExistsPlayer, i18n.Config.KickExistsPlayer));

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
            ForwardMode = ForwardMode.Direct;
            SelectMode = ServerSelectMode.Failover;
            Flags = ServerFlags.None;
            ServerEndPoints = new List<EndPoint>() { new DnsEndPoint("example.net", 25565), new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 233) };
            ProtocolVersion = -1;
            CompressionThreshold = -1;
        }

        
        private async Task<Socket> OpenConnectAsync(EndPoint endPoint)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); ;
            await socket.ConnectAsync(endPoint);
            return socket;
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
                        return await OpenConnectAsync(servers[i]);
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
                return await OpenConnectAsync(ServerEndPoints[ServerIndex.Get()]);
            }
            else if (SelectMode == ServerSelectMode.Random)
            {
                return await OpenConnectAsync(ServerEndPoints[Random.Next(0, ServerEndPoints.Count - 1)]);
            }

            throw new Exception(i18n.Error.NoSocketAvailable);
        }


        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
