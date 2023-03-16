using System.Net;
using System.Xml;

namespace Firewall.Rules
{
    public class BaseNetworkRuleItem
    {
        public bool IsEmpty => IP == null && Mask == null && Port == null;

        public RuleItem<IPAddress> IP { get; set; }

        public IPAddress Mask { get; set; }

        public PortRuleItem Port { get; set; }

        public BaseNetworkRuleItem(RuleItem<IPAddress> ip)
        {
            IP = ip ?? throw new ArgumentNullException(nameof(ip));
        }
        public BaseNetworkRuleItem(PortRuleItem port)
        {
            Port = port ?? throw new ArgumentNullException(nameof(port));
        }
        public BaseNetworkRuleItem(RuleItem<IPAddress> ip, IPAddress mask)
        {
            IP = ip ?? throw new ArgumentNullException(nameof(ip));
            Mask = mask ?? throw new ArgumentNullException(nameof(mask));
        }
        public BaseNetworkRuleItem(RuleItem<IPAddress> ip, IPAddress mask, PortRuleItem port)
        {
            IP = ip ?? throw new ArgumentNullException(nameof(ip));
            Mask = mask ?? throw new ArgumentNullException(nameof(mask));
            Port = port ?? throw new ArgumentNullException(nameof(port));
        }

        internal BaseNetworkRuleItem(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                string? ip = reader.GetAttribute(nameof(IP));
                string? mask = reader.GetAttribute(nameof(Mask));
                string? port = reader.GetAttribute(nameof(Port));

                if (ip != null)
                    IP = new RuleItem<IPAddress>(IPAddress.Parse(ip[0] == '!' ? ip.Substring(1) : ip)) { Invert = ip[0] == '!' };
                if (mask != null)
                    Mask = IPAddress.Parse(mask);
                if (port != null)
                    Port = new PortRuleItem(PortRange.Parse(port[0] == '!' ? port.Substring(1) : port)) { Invert = port[0] == '!' };

                return;
            }

        }

        public void Write(XmlWriter writer, string key)
        {
            writer.WriteStartElement(key);
            if (IP != null)
                writer.WriteAttributeString(nameof(IP), $"{(IP.Invert ? "!" : "")}{IP.Value}");
            if (Mask != null)
                writer.WriteAttributeString(nameof(Mask), Mask.ToString()); //掩码不需要反转吧?
            if (Port != null)
                writer.WriteAttributeString(nameof(Port), $"{(Port.Invert ? "!" : "")}{Port.Value}");
            writer.WriteEndElement();
        }

        internal virtual bool Match(IPAddress ip, int port)
        {
            bool result = false;
            if (IP != null)
                result = Match(ip);
            if (Port != null)
                result = Match(port);

            return result;
        }

        public virtual bool Match(IPAddress other)
        {
            if (IP.Value.AddressFamily != other.AddressFamily)
                throw new ArgumentException("AddressFamily is not match.");

            IPAddress ip = IP.Value;
            if (Mask != null)
            {
                byte[] ipBytes = ip.GetAddressBytes();
                byte[] otherIpBytes = other.GetAddressBytes();
                byte[] maskBytes = other.GetAddressBytes();
                for (int i = 0; i < ipBytes.Length; i++)
                {
                    ipBytes[i] &= maskBytes[i];
                    otherIpBytes[i] &= maskBytes[i];
                    if (IP.Invert ? ipBytes[i] == otherIpBytes[i] : ipBytes[i] != otherIpBytes[i])
                        return false;
                }
                return !IP.Invert;
            }
            else
            {
                return IP.Invert ? !ip.Equals(other) : ip.Equals(other);
            }
        }

        public virtual bool Match(int port)
        {
            return Port.Match(port);
        }

        public virtual bool Match(PortRange port)
        {
            return Port.Match(port);
        }

        public static implicit operator BaseNetworkRuleItem(RuleItem<IPAddress> value) => new BaseNetworkRuleItem(value);

        public static implicit operator BaseNetworkRuleItem(IPAddress value) => new BaseNetworkRuleItem(value);

        public static implicit operator BaseNetworkRuleItem(PortRuleItem value) => new BaseNetworkRuleItem(value);

        public static implicit operator BaseNetworkRuleItem(int value) => new BaseNetworkRuleItem(new PortRuleItem(new PortRange((ushort)value)));

        public static implicit operator BaseNetworkRuleItem(ushort value) => new BaseNetworkRuleItem(new PortRuleItem(new PortRange(value)));
    }
}
