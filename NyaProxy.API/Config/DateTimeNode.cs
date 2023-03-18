using System;
namespace NyaProxy.API
{

    public class DateTimeNode : ConfigNode
    {
        public virtual DateTime Value { get; set; }

        public DateTimeNode(DateTime value)
        {
            Value = value;
        }
        
        public DateTimeNode(DateTime value, string precedingComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment))
                Comment = new ConfigComment(precedingComment);
            Value = value;
        }
        public DateTimeNode(DateTime value, string precedingComment, string inlineComment)
        {
            if (!string.IsNullOrWhiteSpace(precedingComment) && string.IsNullOrWhiteSpace(inlineComment))
                Comment = new ConfigComment(precedingComment, inlineComment);
            Value = value;
        }
        public DateTimeNode(DateTime value, ConfigComment comment)
        {
            if (comment!=null)
                Comment = comment;
            Value = value;
        }

        public static implicit operator DateTime(DateTimeNode node) => node.Value;

        public static implicit operator DateTimeNode(DateTime value) => new DateTimeNode(value);

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
