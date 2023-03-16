using MinecraftProtocol.DataType;
using System.Xml;

namespace Firewall.Rules
{
    public class LoginRule : PacketRule
    {
        public RuleItem<string> PlayerName { get; set; }

        public RuleItem<UUID> PlayerUUID { get; set; }

        public LoginRule() { }

        internal LoginRule(XmlReader reader) : base(reader) { }

        protected internal override object Read(XmlReader reader)
        {
            if (base.Read(reader) == null)
            {
                if (reader.Name == nameof(PlayerName))
                    PlayerName = new RuleItem<string>(reader, (text) => text);
                else if (reader.Name == nameof(PlayerUUID))
                    PlayerUUID = new RuleItem<UUID>(reader, (text) => UUID.Parse(text));
            }

            return null;
        }

        internal override void Write(XmlWriter writer)
        {
            base.Write(writer);
            PlayerName?.Write(writer, nameof(PlayerName));
            PlayerUUID?.Write(writer, nameof(PlayerUUID));
        }
    }
}
