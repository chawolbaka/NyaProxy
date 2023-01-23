using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Extension;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Tommy;

namespace NyaProxy
{
    public class Config
    {
        private static readonly string ServersPath =  "Servers";
        private readonly ConfigFile MainConfig = new ConfigFile(new FileInfo("config.toml"), true);

        public IPEndPoint Bind { get; set; }
        public int ConnectionTimeout { get; set; }
        public int ConnectionThrottle { get; set; }
        public bool TcpFastOpen { get; set; }
        public int NetworkThread { get; set; }
        public Dictionary<string, HostConfig> Hosts { get; set; }

        public bool IsLoad { get; set; }

        public Config()
        {
            Hosts = new ();
        }

        public void Load()
        {
            if (!MainConfig.File.Exists)
                Save();

            TomlNode advanced = MainConfig["advanced"];
            Bind = IPEndPoint.Parse(MainConfig["bind"]);
            ConnectionTimeout = advanced["connection-timeout"];
            ConnectionThrottle = advanced["connection-throttle"];
            TcpFastOpen = advanced["tcp-fast-open"];
            NetworkThread = advanced["network-threads"];

            //Update
            foreach (var server in Hosts.ToArray())
            {
                if (!server.Value.File.Exists)
                    Hosts.Remove(server.Key);

                HostConfig hostConfig = server.Value;
                hostConfig.Reload();
                if (server.Key != hostConfig.Name)
                {
                    Hosts.Remove(server.Key);
                    Hosts.Add(hostConfig.Name, hostConfig);
                }
            }

            //Load new file
            foreach (var file in Directory.GetFiles(ServersPath).Where(f => !f.EndsWith("example.toml") && f.EndsWith(".toml")).Select(x => new FileInfo(x)))
            {
                if (file.Exists && file.Length > 0)
                {
                    ConfigFile cf = new ConfigFile(file, true);
                    if (!cf.RawTable.ContainsKey("host"))
                        continue;

                    string host = cf["host"];
                    if (Hosts.ContainsKey(host))
                    {
                        if (file.FullName == Hosts[host].File.FullName)
                            Hosts[host].Reload();
                        else
                            NyaProxy.Logger.Warn(i18n.Warning.ConfigFileConflict.Replace("{Host}", host, "{FileName}", Hosts[cf["host"]].File.Name));
                    }
                    else
                    {
                        HostConfig hostConfig = new HostConfig(file);
                        hostConfig.Reload();
                        Hosts.Add(host, hostConfig);
                        if (!NyaProxy.Bridges.ContainsKey(hostConfig.Name))
                            NyaProxy.Bridges.TryAdd(hostConfig.Name, new ConcurrentDictionary<Guid, Bridge>());
                    }
                }
            }
            IsLoad = true;
        }


        public void Save()
        {
            if (!Directory.Exists(ServersPath))
                Directory.CreateDirectory(ServersPath);
            if (!MainConfig.File.Exists)
                SetupDefaultConfig();


            MainConfig["bind"] = new TomlString { Value = Bind.ToString(), Comment = i18n.Config.Bind };
            MainConfig["advanced"] = new TomlTable()
            {
                ["connection-timeout"] = new TomlInteger { Value = ConnectionTimeout, Comment = i18n.Config.ConnectionTimeout },
                ["connection-throttle"] = new TomlInteger { Value = ConnectionThrottle, Comment = i18n.Config.ConnectionThrottle },
                ["tcp-fast-open"] = new TomlBoolean { Value = TcpFastOpen, Comment = i18n.Config.TcpFastOpen },
                ["network-threads"] = new TomlInteger { Value = NetworkThread, Comment = i18n.Config.NetworkThread },
            };
            MainConfig.Save();
            foreach (var server in Hosts)
            {
                server.Value.Save();
            }
        }

        private void SetupDefaultConfig()
        {
            Bind = new IPEndPoint(new IPAddress(new byte[] { 0, 0, 0, 0 }), 25565);
            ConnectionTimeout = 1000 * 8;
            ConnectionThrottle = 1000;
            TcpFastOpen = false;
            NetworkThread = Environment.ProcessorCount;
            if (Hosts.Count == 0)
            {
                HostConfig serverConfig =  new HostConfig(new FileInfo(Path.Combine(ServersPath,"example.toml")))
                {
                    Name = "example",
                    ForwardMode = ForwardMode.Direct,
                    SelectMode = ServerSelectMode.Failover,
                    Flags = ServerFlags.None,
                    ServerEndPoints = new List<EndPoint>() 
                    {
                        new DnsEndPoint("example.net", 25565),
                        new IPEndPoint(new IPAddress(new byte[] { 127,0,0,1 }), 233)
                    },
                    ProtocolVersion = -1
                };
                serverConfig.Save();
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(MainConfig.ToString()+Environment.NewLine + Environment.NewLine);
            
            foreach (var server in Hosts)
            {
                sb.AppendLine($"{server.Key}:");
                sb.AppendLine($"\t{server.Value}");
            }
            return sb.ToString();

        }
    }
}
