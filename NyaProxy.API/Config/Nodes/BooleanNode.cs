using System;

namespace NyaProxy.API.Config.Nodes
{

    public class BooleanNode : ConfigNode
    {
        public virtual bool Value { get; set; }

        public BooleanNode(bool value)
        {
            Value = value;
        }

        public BooleanNode(bool value, string precedingComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment))
                Comment = new ConfigComment(precedingComment);
            Value = value;
        }
        public BooleanNode(bool value, string precedingComment, string inlineComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment) && string.IsNullOrWhiteSpace(inlineComment))
                Comment = new ConfigComment(precedingComment, inlineComment);
            Value = value;
        }
        public BooleanNode(bool value, ConfigComment comment)
        {
            if (comment != null)
                Comment = comment;
            Value = value;
        }

        public static implicit operator bool(BooleanNode node) => node.Value;

        public static implicit operator BooleanNode(bool value) => new BooleanNode(value);

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
