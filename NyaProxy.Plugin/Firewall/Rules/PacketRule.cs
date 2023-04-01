using System.Xml;
using NyaGenerator.Equatable;

namespace NyaFirewall.Rules
{
    [Equatable]
    public partial class PacketRule : Rule
    {
        public RuleItem<int> PacketId { get; set; }

        public RuleItem<int> ProtocolVersion { get; set; }

        public PacketRule() : base() { }

        protected override object ReadFromXml(XmlReader reader)
        {
            if (base.ReadFromXml(reader) == null)
            {
                if (reader.Name == nameof(PacketId))
                    return PacketId = new RuleItem<int>(reader, (text) => int.Parse(text));
                else if (reader.Name == nameof(ProtocolVersion))
                    ProtocolVersion = new RuleItem<int>(reader, (text) => int.Parse(text));
            }
            return null;
        }

        internal override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
            PacketId?.WriteXml(writer, nameof(PacketId));
            ProtocolVersion?.WriteXml(writer, nameof(ProtocolVersion));
        }

        internal override List<string> CreateFirstColumns()
        {
            List<string> list = base.CreateFirstColumns();
            list.Add(nameof(PacketId));
            list.Add(nameof(ProtocolVersion));
            return list;
        }

        internal override List<object> CreateRow()
        {
            List<object> row = base.CreateRow();
            row.Add(PacketId);
            row.Add(ProtocolVersion);
            return row;
        }
    }
}
