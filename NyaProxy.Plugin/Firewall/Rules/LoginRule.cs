using MinecraftProtocol.DataType;
using System.Xml;
using NyaGenerator.Equatable;

namespace NyaFirewall.Rules
{
    [Equatable]
    public partial class LoginRule : PacketRule
    {
        public RuleItem<string> PlayerName { get; set; }

        public RuleItem<UUID> PlayerUUID { get; set; }

        public LoginRule() : base() { }

        protected override object ReadFromXml(XmlReader reader)
        {
            if (base.ReadFromXml(reader) == null)
            {
                if (reader.Name == nameof(PlayerName))
                    PlayerName = new RuleItem<string>(reader, (text) => text);
                else if (reader.Name == nameof(PlayerUUID))
                    PlayerUUID = new RuleItem<UUID>(reader, (text) => UUID.Parse(text));
            }

            return null;
        }

        internal override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
            PlayerName?.WriteXml(writer, nameof(PlayerName));
            PlayerUUID?.WriteXml(writer, nameof(PlayerUUID));
        }

        internal override List<string> CreateFirstColumns()
        {
            List<string> list = base.CreateFirstColumns();
            list.Add(nameof(PlayerUUID));
            list.Add(nameof(PlayerName));
            return list;
        }

        internal override List<object> CreateRow()
        {
            List<object> row = base.CreateRow();
            row.Add(PacketId);
            row.Add(PlayerName);
            return row;
        }

    }
}
