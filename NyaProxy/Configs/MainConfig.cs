using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NyaProxy.API;
using NyaProxy.API.Config;
using NyaProxy.API.Config.Nodes;

namespace NyaProxy.Configs
{
    [ConfigFile("config")]
    public class MainConfig : Config, IDefaultConfig, IManualConfig
    {
        public IPEndPoint[] Bind { get; set; }
        public bool TcpFastOpen { get; set; }
        public int NetworkThread { get; set; }

        public LogFile LogFile;

        public bool EnableBlockingQueue;

        public bool EnableStickyPacket;
        public bool EnableStickyPool;
        public int StickyPacketLimit;
        public int NumberOfStickyPoolBuffers;
        public int StickyPoolBufferLength;

        public bool EnableReceivePool;
        public int NumberOfReceivePoolBuffers;
        public int ReceivePoolBufferLength;

        public MainConfig() : base("MainConfig")
        {
            SetDefault();
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
                LogFile = new LogFile() 
                {
                    Enable    = (bool)logFile["enable"],
                    Format    = (string)logFile["format"],
                    Directory = (string)logFile["directory"]
                };
            }

            if (reader.ContainsKey("sticky-packet"))
            {
                ObjectNode stickyPacket = reader.ReadObjectProperty("sticky-packet");

                EnableStickyPacket = (bool)stickyPacket["enable"];
                StickyPacketLimit = (int)stickyPacket["limit"];

                if (stickyPacket.ContainsKey("enable-pool"))
                {
                    EnableStickyPool = (bool)stickyPacket["enable-pool"];
                    if (stickyPacket.ContainsKey("pool-buffers"))
                        NumberOfStickyPoolBuffers = (int)stickyPacket["pool-buffers"];
                    if (stickyPacket.ContainsKey("pool-buffer-length"))
                        StickyPoolBufferLength = (int)stickyPacket["pool-buffer-length"];

                    if (NumberOfStickyPoolBuffers <= 1 || StickyPoolBufferLength <= 64)
                        EnableStickyPool = false;
                }

       

            }
            if (reader.ContainsKey("advanced"))
            {
                ObjectNode advanced = reader.ReadObjectProperty("advanced");
                NetworkThread       = (int)advanced["network-threads"];
                TcpFastOpen         = advanced.ContainsKey("tcp-fast-open") ? (bool)advanced["tcp-fast-open"] : false;
                EnableBlockingQueue = advanced.ContainsKey("enable-blocking-queue") ? (bool)advanced["enable-blocking-queue"] : true;

                if (advanced.ContainsKey("enable-receive-pool"))
                {
                    EnableReceivePool = (bool)advanced["enable-receive-pool"];
                    if (advanced.ContainsKey("receive-pool-buffers"))
                        NumberOfReceivePoolBuffers = (int)advanced["receive-pool-buffers"];
                    if (advanced.ContainsKey("receive-pool-buffer-length"))
                        ReceivePoolBufferLength = (int)advanced["receive-pool-buffer-length"];

                    if (NumberOfReceivePoolBuffers <= 1 || ReceivePoolBufferLength <= 64)
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
                    ["enable"]    = new BooleanNode(LogFile.Enable, i18n.Config.EnableLogFile),
                    ["format"]    = new StringNode(LogFile.Format, i18n.Config.LogFormat),
                    ["directory"] = new StringNode(LogFile.Directory, i18n.Config.LogFileDirectory)
                }
            });


            writer.WriteProperty("sticky-packet", new ObjectNode()
            {
                Nodes = new Dictionary<string, ConfigNode>()
                {
                    ["enable"]             = new BooleanNode(EnableStickyPacket, i18n.Config.EnableStickyPacket),
                    ["limit"]              = new NumberNode(StickyPacketLimit, i18n.Config.StickyPacketLimit),
                    ["enable-pool"]        = new BooleanNode(EnableStickyPool, i18n.Config.EnableStickyPool),
                    ["pool-buffers"]       = new NumberNode(NumberOfStickyPoolBuffers, i18n.Config.NumberOfStickyPoolBuffers),
                    ["pool-buffer-length"] = new NumberNode(StickyPoolBufferLength, i18n.Config.StickyPoolBufferLength),
                }
            });

            writer.WriteProperty("advanced", new ObjectNode()
            {
                Nodes = new Dictionary<string, ConfigNode>()
                {
                    ["network-threads"]     = new NumberNode(NetworkThread, i18n.Config.NetworkThread),
                    ["tcp-fast-open"]       = new BooleanNode(TcpFastOpen,  i18n.Config.TcpFastOpen),
                    ["enable-blocking-queue"] = new BooleanNode(EnableBlockingQueue, i18n.Config.EnableBlockingQueue),

                    ["enable-receive-pool"] = new BooleanNode(EnableReceivePool,   i18n.Config.EnableReceivePool),
                    ["receive-pool-buffers"]  = new NumberNode(NumberOfReceivePoolBuffers,  i18n.Config.ReceivePoolBuffers),
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

            EnableStickyPacket = true;
            EnableStickyPool = true;
            StickyPacketLimit = 32;
            NumberOfStickyPoolBuffers = 1024;
            StickyPoolBufferLength = 1460;

            EnableReceivePool = true;
            NumberOfReceivePoolBuffers = 1024;
            ReceivePoolBufferLength = 1024 * 8;
            LogFile = new LogFile() { Enable = true, Format = "yyyy-MM-dd", Directory = "log" };
        }
    }
}
