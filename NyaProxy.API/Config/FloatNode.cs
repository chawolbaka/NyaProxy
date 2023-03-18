using System;
namespace NyaProxy.API
{

    public class FloatNode : ConfigNode
    {
        public virtual float Value { get; set; }

        public FloatNode(float value)
        {
            Value = value;
        }
        
        public FloatNode(float value, string precedingComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment))
                Comment = new ConfigComment(precedingComment);
            Value = value;
        }
        public FloatNode(float value, string precedingComment, string inlineComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment) && string.IsNullOrWhiteSpace(inlineComment))
                Comment = new ConfigComment(precedingComment, inlineComment);
            Value = value;
        }
        public FloatNode(float value, ConfigComment comment)
        {
            if (comment!=null)
                Comment = comment;
            Value = value;
        }

        public static implicit operator float(FloatNode node) => node.Value;

        public static implicit operator FloatNode(float value) => new FloatNode(value);

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
