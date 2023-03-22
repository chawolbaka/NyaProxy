using System.Xml;
using NyaGenerator.Equatable;

namespace Firewall.Rules
{
    [Equatable]
    public partial class PacketRule : Rule
    {
        public RuleItem<int> PacketId { get; set; }

        public PacketRule() { }

        protected override object Read(XmlReader reader)
        {
            if (base.Read(reader) == null && reader.Name == nameof(PacketId))
                return PacketId = new RuleItem<int>(reader, (text) => int.Parse(text));

            return null;
        }

        internal override void Write(XmlWriter writer)
        {
            base.Write(writer);
            PacketId?.Write(writer, nameof(PacketId));
        }

        internal override List<string> CreateFirstColumns()
        {
            List<string> list = base.CreateFirstColumns();
            list.Add(nameof(PacketId));
            return list;
        }

        internal override List<object> CreateRow()
        {
            List<object> row = base.CreateRow();
            row.Add(PacketId);
            return row;
        }
    }
}
