namespace Firewall.Rules
{
    public class PortRuleItem : RuleItem<PortRange>
    {
        public PortRuleItem(PortRange value) : base(value)
        {

        }

        public virtual bool Match(int port)
        {
            return Value.Equals(port);
        }

        public static implicit operator PortRuleItem(PortRange value) => new PortRuleItem(value);

        public static implicit operator PortRuleItem(int value) => new PortRuleItem(new PortRange((ushort)value));

        public static implicit operator PortRuleItem(ushort value) => new PortRuleItem(new PortRange(value));
    }
}
