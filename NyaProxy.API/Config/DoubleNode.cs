using System;
namespace NyaProxy.API
{

    public class DoubleNode : ConfigNode
    {
        public virtual double Value { get; set; }

        public DoubleNode(double value)
        {
            Value = value;
        }
        
        public DoubleNode(double value, string precedingComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment))
                Comment = new ConfigComment(precedingComment);
            Value = value;
        }
        public DoubleNode(double value, string precedingComment, string inlineComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment) && string.IsNullOrWhiteSpace(inlineComment))
                Comment = new ConfigComment(precedingComment, inlineComment);
            Value = value;
        }
        public DoubleNode(double value, ConfigComment comment)
        {
            if (comment!=null)
                Comment = comment;
            Value = value;
        }

        public static implicit operator double(DoubleNode node) => node.Value;

        public static implicit operator DoubleNode(double value) => new DoubleNode(value);

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
