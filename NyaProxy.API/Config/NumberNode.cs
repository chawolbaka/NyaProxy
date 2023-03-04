using System;
namespace NyaProxy.API
{

    public class NumberNode : ConfigNode
    {
        public virtual long Value { get; set; }

        public NumberNode(long value)
        {
            Value = value;
        }
        
        public NumberNode(long value, string precedingComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment))
                Comment = new ConfigComment(precedingComment);
            Value = value;
        }
        public NumberNode(long value, string precedingComment, string inlineComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment) && string.IsNullOrWhiteSpace(inlineComment))
                Comment = new ConfigComment(precedingComment, inlineComment);
            Value = value;
        }
        public NumberNode(long value, ConfigComment comment)
        {
            if (comment!=null)
                Comment = comment;
            Value = value;
        }

        public static implicit operator long(NumberNode node) => node.Value;

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
