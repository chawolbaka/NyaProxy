using System;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;
using MinecraftProtocol.DataType.Forge;
using NyaProxy.API.Config;
using NyaProxy.API.Config.Nodes;

namespace Motd
{
    public class MotdConfig : Config, IManualConfig, IDefaultConfig
    {
        public string Host { get; set; }

        public PingReply PingReply { get; set; }

        private static int Index;

        public MotdConfig() : base(Index++.ToString())
        {
            PingReply = new PingReply();
        }

        public void Read(ConfigReader reader)
        {
            PingReply pingReply = new PingReply();

            Host = reader.ReadStringProperty("host");
            string motd = reader.ReadStringProperty("motd");
            if (motd.StartsWith('{') && motd.EndsWith('}'))
                pingReply.Motd = ChatComponent.Deserialize(motd);
            else
                pingReply.Motd = ChatComponent.Parse(motd);


            if (reader.TryReadObject("version", out var version))
                pingReply.Version = new PingReply.VersionPayload() { Name = (string)version["name"], Protocol = ReadProtocolVersionByConfigNode(version["protocol"]) };
            if (reader.TryReadObject("forge", out var forge))
                pingReply.Forge = new PingReply.ForgePayLoad((string)forge["type"], ModList.Parse((string)forge["mods"]));
            if (reader.TryReadString("icon", out string icon))
                pingReply.Icon = icon;


            pingReply.Player = new PingReply.PlayerPayload() { Max = (int)reader.ReadNumberProperty("max-player") };
            if (reader.ContainsKey("player-samples"))
            {
                foreach (StringNode playerName in reader.ReadArrayProperty("player-samples"))
                {
                    pingReply.Player.Samples.Add(new PingReply.PlayerSample(UUID.GetFromPlayerName(playerName).ToString(), playerName));
                }
            }
            else
            {
                //我序列化那边偷懒了，所以这边需要改成null，否则会有一个空的属性
                pingReply.Player.Samples = null;
            }
            PingReply = pingReply;

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
            writer.WriteProperty("host", Host);
            writer.WriteProperty("motd", PingReply.Motd.ToString());
            writer.WriteProperty("max-player", PingReply.Player.Max);

            writer.WriteProperty("version", new ObjectNode(new Dictionary<string, ConfigNode>()
            {
                ["name"] = new StringNode(PingReply.Version.Name),
                ["protocol"] = new NumberNode(PingReply.Version.Protocol)
            }));

            if (PingReply.Forge != null)
                writer.WriteProperty("forge", new ObjectNode(new Dictionary<string, ConfigNode>()
                {
                    ["type"] = new StringNode(PingReply.Forge.Type),
                    ["mods"] = new StringNode(string.Join(',', PingReply.Forge.ModList))
                }));

            if (PingReply.Player.Samples != null && PingReply.Player.Samples.Count > 0)
                writer.WriteProperty("player-samples", new ArrayNode(PingReply.Player.Samples.Select(p => new StringNode( new StringNode(p.Name)))));


            if (!string.IsNullOrWhiteSpace(PingReply.Icon))
                writer.WriteProperty("icon", PingReply.Icon);
        }

        public void SetDefault()
        {
            Host = "example";
            PingReply.Motd = new ChatComponent("A example motd");
            PingReply.Player.Max = 233;
            PingReply.Player.Samples = new List<PingReply.PlayerSample>(new PingReply.PlayerSample[] {
                new PingReply.PlayerSample(UUID.NewUUID().ToString(), "example 01"),
                new PingReply.PlayerSample(UUID.NewUUID().ToString(), "example 02"),
                new PingReply.PlayerSample(UUID.NewUUID().ToString(), "example 03") });
            PingReply.Version.Name = "1.12.2";
            PingReply.Version.Protocol = ProtocolVersions.V1_12_2;
        }
    }
}
