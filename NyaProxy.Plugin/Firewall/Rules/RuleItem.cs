using System.Xml;

namespace Firewall.Rules
{
    public class RuleItem<T>
    {
        public bool Invert { get; set; }

        public T Value { get; set; }

        public RuleItem(T value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal RuleItem(XmlReader reader, Func<string, T> createValue)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                    return;

                if (reader.HasAttributes)
                    Invert = bool.Parse(reader.GetAttribute(nameof(Invert)) ?? bool.FalseString);

                if (reader.NodeType == XmlNodeType.Text)
                    Value = createValue(reader.Value);
            }
        }

        internal virtual void Write(XmlWriter writer, string key)
        {
            writer.WriteStartElement(key);
            if (Invert)
                writer.WriteAttributeString(nameof(Invert), Invert.ToString());
            writer.WriteValue(Value is IConvertible ? Value : Value.ToString());
            writer.WriteEndElement();
        }

        public virtual bool Match(T other)
        {
            if (Value == null || other == null)
                return Invert;

            if (Value is string str && !string.IsNullOrEmpty(str) && str == other.ToString())
                return !Invert;
            else if (Value is IEquatable<T> ie && ie.Equals(other) || Value.Equals(other))
                return !Invert;
            else
                return Invert;
        }

        public override bool Equals(object? obj)
        {
            return Value.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Invert ? "!" : "" + Value.ToString();
        }

        public static implicit operator RuleItem<T>(T value) => new RuleItem<T>(value);
    }
}
