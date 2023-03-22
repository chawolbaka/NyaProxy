﻿using Firewall.Rules;
using StringTables;
using System.Collections;
using System.Xml;

namespace Firewall.Tables
{
    public class Table<T> : IEnumerable<T> where T : Rule, new()
    {
        public virtual bool IsEmpty => Rules.Count == 0;

        public virtual LinkedList<T> Rules { get; set; }

        public Table()
        {
            Rules = new ();
        }

        internal Table(XmlReader reader, Func<XmlReader, T> create, string tableName) : this()
        {
            do
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == nameof(PacketRule))
                    Rules.AddLast(create(reader));

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == tableName)
                    return;
            } while (reader.Read());
        }

        internal virtual void WriteXml(XmlWriter writer)
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

                rule.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        internal virtual string ToTable()
        {
            if (Rules.Count == 0)
                return string.Empty;
            StringTable table = new StringTable(Rules.First.Value.CreateFirstColumns());
            foreach (var rule in Rules)
            {
                table.AddRow(rule.CreateRow().Select(x => x == null ? "Any" : x.ToString()).ToArray());
            }
            return table.ToMinimalString();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Rules).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Rules).GetEnumerator();
        }
    }
}
