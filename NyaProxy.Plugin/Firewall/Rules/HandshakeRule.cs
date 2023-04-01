using MinecraftProtocol.Packets.Client;
using System.Xml;
using NyaGenerator.Equatable;

namespace NyaFirewall.Rules
{
    [Equatable]
    public partial class HandshakeRule : PacketRule
    {
        public RuleItem<string> ServerAddress { get; set; }

        public RuleItem<ushort> ServerPort { get; set; }

        public RuleItem<HandshakeState> NextState { get; set; }

        public HandshakeRule() : base() { }

        protected override object ReadFromXml(XmlReader reader)
        {
            if (base.ReadFromXml(reader) == null)
            {
                if (reader.Name == nameof(ServerAddress))
                    ServerAddress = new RuleItem<string>(reader, (text) => text);
                else if (reader.Name == nameof(ServerPort))
                    ServerPort = new RuleItem<ushort>(reader, (text) => ushort.Parse(text));
                else if (reader.Name == nameof(NextState))
                    NextState = new RuleItem<HandshakeState>(reader, (text) => Enum.Parse<HandshakeState>(text));

            }

            return null;
        }

        internal override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
            ServerAddress?.WriteXml(writer, nameof(ServerAddress));
            ServerPort?.WriteXml(writer, nameof(ServerPort));
            NextState?.WriteXml(writer, nameof(NextState));
        }

        internal override List<string> CreateFirstColumns()
        {
            List<string> list = base.CreateFirstColumns();
            list.Add(nameof(ServerAddress));
            list.Add(nameof(ServerPort));
            list.Add(nameof(NextState));
            return list;
        }

        internal override List<object> CreateRow()
        {
            List<object> row = base.CreateRow();
            row.Add(ServerAddress);
            row.Add(ServerPort);
            row.Add(NextState);
            return row;
        }

    }
}
