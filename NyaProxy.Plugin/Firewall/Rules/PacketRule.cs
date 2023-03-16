using System.Xml;

namespace Firewall.Rules
{
    public class PacketRule : Rule
    {
        public RuleItem<int> PacketId { get; set; }

        public PacketRule() { }

        internal PacketRule(XmlReader reader) : base(reader) { }

        internal protected override object Read(XmlReader reader)
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
    }
}
