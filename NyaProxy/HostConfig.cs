using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.API.Enum;
using Tommy;

namespace NyaProxy
{
    public class HostConfig : ConfigFile, IHost
    {
        public string Name { get; set; }
        public ServerSelectMode SelectMode { get; set; }
        public ForwardMode ForwardMode { get; set; }
        public List<EndPoint> ServerEndPoints { get; set; }
        public int CompressionThreshold { get; set; }
        public int ProtocolVersion { get; set; }
        public bool KickExistsPlayer { get; set; }
        public ServerFlags Flags { get; set; }

        public HostConfig(FileInfo file) : base(file)
        {
            ServerEndPoints = new();
        }

        public override void Reload()
        {
            base.Reload();
            Name = base["host"];
            ProtocolVersion = base["server-version"];
            Flags = Enum.Parse<ServerFlags>(base["server-flags"]);
            SelectMode = Enum.Parse<ServerSelectMode>(base["server-select-mode"]);
            ForwardMode = Enum.Parse<ForwardMode>(base["player-info-forwarding-mode"]);
            CompressionThreshold = base["compression-threshold"];
            KickExistsPlayer = base["kick-exists-player"];
           
            foreach (TomlNode node in base["servers"])
            {
                string server = node.AsString;
                if (IPEndPoint.TryParse(server, out IPEndPoint ipEndPoint))
                {
                    ServerEndPoints.Add(ipEndPoint);
                }
                else
                {
                    string[] s = server.Split(':');
                    if (s.Length == 2 && ushort.TryParse(s[1], out ushort port))
                    {
                        ServerEndPoints.Add(new DnsEndPoint(s[0], port));
                    }
                }

            }
        }

        public override void Save()
        {
            base["host"] = new TomlString(Name, i18n.Config.Host);
            base["servers"] = GetServers();
            base["server-select-mode"] = new TomlString(SelectMode, i18n.Config.SelectMode);
            base["server-version"] = new TomlInteger(ProtocolVersion, i18n.Config.ProtocolVersion);
            base["server-flags"] = new TomlString(Flags, i18n.Config.ServerFlags);
            base["compression-threshold"] = new TomlInteger(CompressionThreshold, i18n.Config.CompressionThreshold);
            base["player-info-forwarding-mode"] = new TomlString(ForwardMode, i18n.Config.ForwardMode);
            base["kick-exists-player"] = new TomlBoolean(KickExistsPlayer, i18n.Config.KickExistsPlayer);
        }

        private TomlNode[] GetServers()
        {
            TomlNode[] nodes = new TomlNode[ServerEndPoints.Count];
            for (int i = 0; i < ServerEndPoints.Count; i++)
            {
                if (ServerEndPoints[i] is DnsEndPoint dnsEndPoint)
                    nodes[i] = $"{dnsEndPoint.Host}:{dnsEndPoint.Port}";
                else
                    nodes[i] = ServerEndPoints[i].ToString();
            }
            return nodes;
        }

        private static Random Random = new Random();
        private int LastServerIndex = 0;
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
                if (LastServerIndex >= ServerEndPoints.Count)
                    LastServerIndex = 0;
                return await OpenConnectAsync(ServerEndPoints[LastServerIndex++]);
            }
            else if (SelectMode == ServerSelectMode.Random)
            {
                return await OpenConnectAsync(ServerEndPoints[Random.Next(0, ServerEndPoints.Count - 1)]);
            }

            throw new Exception(i18n.Error.NoSocketAvailable);
        }

        private async Task<Socket> OpenConnectAsync(EndPoint endPoint)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); ;
            await socket.ConnectAsync(endPoint);
            return socket;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        

    }
}
