using System;
using System.Xml;

namespace Firewall.Rules
{
    public class Rule
    {
        public bool Disabled { get; set; }

        public RuleItem<string> Host { get; set; }

        public BaseNetworkRuleItem Source { get; set; }

        public BaseNetworkRuleItem Destination { get; set; }

        public RuleAction Action { get; set; }

        public string Description { get; set; }

        public Rule() { }

        internal Rule(XmlReader reader)
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
                        Read(reader);
                    }

                }

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == GetType().Name)
                    return;
            } while (reader.Read());
        }

        internal protected virtual object Read(XmlReader reader)
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

        internal virtual void Write(XmlWriter writer)
        {
            Host?.Write(writer, nameof(Host));

            if (Source != null && !Source.IsEmpty)
                Source?.Write(writer, nameof(Source));
            if (Destination != null && !Destination.IsEmpty)
                Destination?.Write(writer, nameof(Destination));
        }
    }
}
