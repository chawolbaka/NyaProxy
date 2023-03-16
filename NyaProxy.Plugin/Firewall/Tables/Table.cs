using Firewall.Rules;
using System.Xml;

namespace Firewall.Tables
{
    public class Table<T> where T : Rule, new()
    {
        public List<T> Rules { get; set; }

        public Table()
        {
            Rules = new List<T>();
        }

        internal Table(XmlReader reader, Func<XmlReader, T> create, string tableName)
        {
            Rules = new List<T>();
            do
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(PacketRule))
                    Rules.Add(create(reader));

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == tableName)
                    return;
            } while (reader.Read());
        }

        public void Write(XmlWriter writer)
        {
            string key = typeof(T).Name;
            foreach (var rule in Rules)
            {
                writer.WriteStartElement(key);
                if (rule.Disabled)
                    writer.WriteAttributeString(nameof(rule.Disabled), rule.Disabled.ToString());

                writer.WriteAttributeString(nameof(rule.Action), rule.Action.ToString());

                if (!string.IsNullOrWhiteSpace(rule.Description))
                    writer.WriteAttributeString(nameof(rule.Destination), rule.Description);

                rule.Write(writer);
                writer.WriteEndElement();
            }
        }
    }
}
