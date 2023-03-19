using System;

namespace NyaProxy.API.Config.Nodes
{
    public abstract partial class ConfigNode
    {
        public virtual ConfigComment Comment { get; set; }

        public static explicit operator bool(ConfigNode node)     => ((BooleanNode)node).Value;
        public static explicit operator long(ConfigNode node)     => ((NumberNode)node).Value;
        public static explicit operator double(ConfigNode node)   => ((DoubleNode)node).Value;
        public static explicit operator DateTime(ConfigNode node) => ((DateTimeNode)node).Value;
        public static explicit operator string(ConfigNode node)   => ((StringNode)node).Value;

        public override string ToString()
        {
            return GetType().GetProperty("Value").GetValue(this).ToString();
        }
    }
}
