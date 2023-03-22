using StringTables;
using System;
using System.Text;
using System.Xml;
using NyaGenerator.Equatable;

namespace Firewall.Rules
{
    [Equatable]
    public partial class Rule
    {
        [IgnoreEquality]
        public bool Disabled { get; set; }

        public RuleItem<string> Host { get; set; }

        public BaseNetworkRuleItem Source { get; set; }

        public BaseNetworkRuleItem Destination { get; set; }

        public RuleAction Action { get; set; }

        public string Description { get; set; }

        public Rule() { }
       
        internal T ReadFromXml<T>(XmlReader reader) where T: Rule
        {
            do
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == GetType().Name)
                    {
                        Action = Enum.Parse<RuleAction>(reader.GetAttribute(nameof(Action))); //如果有问题就直接报错阻止该配置被创建，因为这是必选项
                        Disabled = bool.Parse(reader.GetAttribute(nameof(Disabled)) ?? bool.FalseString);
                        Description = reader.GetAttribute(nameof(Description));
                    }
                    else
                    {
                        ReadFromXml(reader);
                    }

                }

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == GetType().Name)
                    return (T)this;
            } while (reader.Read());
            return (T)this;
        }
        
        protected virtual object ReadFromXml(XmlReader reader)
        {
            if (reader.Name == nameof(Host))
                return Host = new RuleItem<string>(reader, (text) => text);
            else if (reader.Name == nameof(Source))
                return Source = new BaseNetworkRuleItem(reader);
            else if (reader.Name == nameof(Destination))
                return Destination = new BaseNetworkRuleItem(reader);
            else
                return null;
        }

        internal virtual void WriteXml(XmlWriter writer)
        {
            Host?.WriteXml(writer, nameof(Host));

            if (Source != null && !Source.IsEmpty)
                Source?.WriteXml(writer, nameof(Source));
            if (Destination != null && !Destination.IsEmpty)
                Destination?.WriteXml(writer, nameof(Destination));
        }

        internal virtual List<string> CreateFirstColumns()
        {
            List<string> list = new List<string>
            {
                nameof(Action),
                nameof(Host),
                nameof(Source),
                nameof(Destination)
            };
            return list;
        }
        internal virtual List<object> CreateRow()
        {
            return new List<object> { Action, Host, Source, Destination};
        }

    }
}
