using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NyaProxy.API;
using NyaProxy.API.Config;
using NyaProxy.API.Config.Nodes;

namespace NyaProxy.Configs
{
    [ConfigFile("config.toml")]
    public class MainConfig : Config, IDefaultConfig, IManualConfig
    {
        public IPEndPoint[] Bind { get; set; }
        public bool TcpFastOpen { get; set; }
        public int NetworkThread { get; set; }

        LogFile LogFile { get; set; }


        public bool EnableBlockingQueue;
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

            if (reader.ContainsKey("log-file"))
            {
                ObjectNode logFile = reader.ReadObjectProperty("log-file");
                NyaProxy.Logger.LogFile = new LogFile() 
                {
                    Enable    = (bool)logFile["enable"],
                    Format    = (string)logFile["format"],
                    Directory = (string)logFile["directory"]
                };
            }

            if (reader.ContainsKey("advanced"))
            {
                ObjectNode advanced = reader.ReadObjectProperty("advanced");
                NetworkThread       = (int)advanced["network-threads"];
                TcpFastOpen         = advanced.ContainsKey("tcp-fast-open") ? (bool)advanced["tcp-fast-open"] : false;
                EnableBlockingQueue = advanced.ContainsKey("enable-blocking-queue") ? (bool)advanced["enable-blocking-queue"] : true;

                if (advanced.ContainsKey("enable-receive-pool"))
                {
                    EnableReceivePool       = (bool)advanced["enable-receive-pool"];
                    ReceivePoolBufferCount  = advanced.ContainsKey("receive-pool-buffer-count")  ? (int)advanced["receive-pool-buffer-count"] : 1024;
                    ReceivePoolBufferLength = advanced.ContainsKey("receive-pool-buffer-length") ? (int)advanced["receive-pool-buffer-length"]: 65536;

                    if (ReceivePoolBufferCount <= 1 || ReceivePoolBufferLength <= 64)
                        EnableReceivePool = false;
                }

            }
        }

        public void Write(ConfigWriter writer)
        {
            if (Bind.Length > 1)
                writer.WriteProperty("bind", new ArrayNode(Bind.Select(b => new StringNode(b.ToString(), i18n.Config.Bind))));
            else
                writer.WriteProperty("bind", new StringNode(Bind[0].ToString(), i18n.Config.Bind));

            writer.WriteProperty("log-file", new ObjectNode() 
            {
                Nodes = new Dictionary<string, ConfigNode>()
                {
                    ["enable"]    = new BooleanNode(LogFile.Enable, i18n.Config.EnableReceivePool),
                    ["format"]    = new StringNode(LogFile.Format, i18n.Config.EnableReceivePool),
                    ["directory"] = new StringNode(LogFile.Directory, i18n.Config.EnableReceivePool)
                }
            });

            writer.WriteProperty("advanced", new ObjectNode()
            {
                Nodes = new Dictionary<string, ConfigNode>()
                {
                    ["network-threads"]     = new NumberNode(NetworkThread, i18n.Config.NetworkThread),
                    ["tcp-fast-open"]       = new BooleanNode(TcpFastOpen,  i18n.Config.TcpFastOpen),
                    ["enable-blocking-queue"] = new BooleanNode(EnableBlockingQueue, i18n.Config.EnableBlockingQueue),
                    ["enable-receive-pool"]   = new BooleanNode(EnableReceivePool,   i18n.Config.EnableReceivePool),
                    ["receive-pool-buffer-count"]  = new NumberNode(ReceivePoolBufferCount,  i18n.Config.ReceivePoolBufferCount),
                    ["receive-pool-buffer-length"] = new NumberNode(ReceivePoolBufferLength, i18n.Config.ReceivePoolBufferLength)
                }
            });;
        }

        public void SetDefault()
        {
            Bind = new IPEndPoint[] { new IPEndPoint(new IPAddress(new byte[] { 0, 0, 0, 0 }), 25565) };
            TcpFastOpen = false;
            NetworkThread = Environment.ProcessorCount;
            EnableBlockingQueue = true;
            EnableReceivePool = true;
            ReceivePoolBufferCount = 1024;
            ReceivePoolBufferLength = 65536;
            LogFile = new LogFile() { Enable = true, Format = "yyyy-MM-dd", Directory = "log" };
        }
    }
}
