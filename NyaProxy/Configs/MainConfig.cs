using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NyaProxy.API;

namespace NyaProxy.Configs
{
    [ConfigFile("config.toml")]
    public class MainConfig : Config, IDefaultConfig, IManualConfig
    {
        public IPEndPoint[] Bind { get; set; }
        public int ConnectionThrottle { get; set; }
        public bool TcpFastOpen { get; set; }
        public int NetworkThread { get; set; }
        
        public MainConfig() : base("MainConfig")
        {
        }

        public void Read(ConfigReader reader)
        {
            ConfigNode bind = reader.ReadProperty("bind");
            if(bind is ConfigArray array)
            {
                List<IPEndPoint> bindList = new List<IPEndPoint>();
                foreach (var node in array)
                {
                    if (IPEndPoint.TryParse(node.ToString(), out IPEndPoint iPEndPoint))
                        bindList.Add(iPEndPoint);
                }
                Bind = bindList.ToArray();
            }
            else
            {
                Bind = new IPEndPoint[] { IPEndPoint.Parse(bind.ToString()) };
            }

            ConfigObject advanced = reader.ReadObject("advanced");
            ConnectionThrottle    = (int)advanced["connection-throttle"];
            TcpFastOpen           = (bool)advanced["tcp-fast-open"];
            NetworkThread         = (int)advanced["network-threads"];
        }

        public void Write(ConfigWriter writer)
        {
            if (Bind.Length > 1)
                writer.WriteProperty("bind", new ConfigArray(Bind.Select(b => new StringNode(b.ToString(), i18n.Config.Bind))));
            else
                writer.WriteProperty("bind", new StringNode(Bind[0].ToString(), i18n.Config.Bind));

            writer.WriteProperty("advanced", new ConfigObject()
            {
                Nodes = new Dictionary<string, ConfigNode>()
                {
                    ["connection-throttle"] = new NumberNode(ConnectionThrottle, i18n.Config.ConnectionThrottle),
                    ["tcp-fast-open"]       = new BooleanNode(TcpFastOpen, i18n.Config.TcpFastOpen),
                    ["network-threads"]     = new NumberNode(NetworkThread, i18n.Config.NetworkThread)
                }
            });
        }

        public void SetDefault()
        {
            Bind = new IPEndPoint[] { new IPEndPoint(new IPAddress(new byte[] { 0, 0, 0, 0 }), 25565) };
            ConnectionThrottle = 1000;
            TcpFastOpen = false;
            NetworkThread = Environment.ProcessorCount;
        }
    }
}
