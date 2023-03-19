using System;

namespace NyaProxy.API.Config.Nodes
{
    public class StringNode : ConfigNode
    {
        public virtual string Value { get; set; }

        public StringNode(string value)
        {
            Value = value;
        }

        public StringNode(string value, string precedingComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment))
                Comment = new ConfigComment(precedingComment);
            Value = value;
        }
        public StringNode(string value, string precedingComment, string inlineComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment) && string.IsNullOrWhiteSpace(inlineComment))
                Comment = new ConfigComment(precedingComment, inlineComment);
            Value = value;
        }
        public StringNode(string value, ConfigComment comment)
        {
            if (comment != null)
                Comment = comment;
            Value = value;
        }

        public static implicit operator string(StringNode node) => node.Value;

        public static implicit operator StringNode(string value) => new StringNode(value);

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
