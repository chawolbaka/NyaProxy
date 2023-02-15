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
        public bool TcpFastOpen { get; set; }
        public int NetworkThread { get; set; }

        public bool EnableReceivePool;
        public int ReceivePoolBufferCount;
        public int ReceivePoolBufferLength;

        public MainConfig() : base("MainConfig")
        {
            EnableReceivePool = true;
            ReceivePoolBufferCount = 1024;
            ReceivePoolBufferLength = 65536;
        }

        public void Read(ConfigReader reader)
        {
            ConfigNode bind = reader.ReadProperty("bind");
            if(bind is ArrayNode array)
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

            if (!reader.ContainsKey("advanced"))
                return;

            ObjectNode advanced   = reader.ReadObjectProperty("advanced");
            NetworkThread           = (int)advanced["network-threads"];
            TcpFastOpen             = advanced.ContainsKey("tcp-fast-open") ? (bool)advanced["tcp-fast-open"] : false;

            if (!advanced.ContainsKey("enable-receive-pool"))
                return;

            EnableReceivePool       = (bool)advanced["enable-receive-pool"];
            ReceivePoolBufferCount  = (int)advanced["receive-pool-buffer-count"];
            ReceivePoolBufferLength = (int)advanced["receive-pool-buffer-length"];

            if (ReceivePoolBufferCount <= 1 || ReceivePoolBufferLength <= 64)
                EnableReceivePool = false;

        }

        public void Write(ConfigWriter writer)
        {
            if (Bind.Length > 1)
                writer.WriteProperty("bind", new ArrayNode(Bind.Select(b => new StringNode(b.ToString(), i18n.Config.Bind))));
            else
                writer.WriteProperty("bind", new StringNode(Bind[0].ToString(), i18n.Config.Bind));

            writer.WriteProperty("advanced", new ObjectNode()
            {
                Nodes = new Dictionary<string, ConfigNode>()
                {
                    ["network-threads"]     = new NumberNode(NetworkThread, i18n.Config.NetworkThread),
                    ["tcp-fast-open"]       = new BooleanNode(TcpFastOpen, i18n.Config.TcpFastOpen),
                    ["enable-receive-pool"] = new BooleanNode(EnableReceivePool),
                    ["receive-pool-buffer-count"]  = new NumberNode(ReceivePoolBufferCount),
                    ["receive-pool-buffer-length"] = new NumberNode(ReceivePoolBufferLength)
                }
            });
        }

        public void SetDefault()
        {
            Bind = new IPEndPoint[] { new IPEndPoint(new IPAddress(new byte[] { 0, 0, 0, 0 }), 25565) };
            TcpFastOpen = false;
            NetworkThread = Environment.ProcessorCount;
            EnableReceivePool = true;
            ReceivePoolBufferCount = 1024;
            ReceivePoolBufferLength = 65536;
        }
    }
}
