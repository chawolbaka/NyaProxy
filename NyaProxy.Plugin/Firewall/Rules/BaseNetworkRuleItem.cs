using System;
using System.Net;
using System.Text;
using System.Xml;
using NyaGenerator.Equatable;

namespace Firewall.Rules
{
    [Equatable]
    public partial class BaseNetworkRuleItem
    {
        [IgnoreEquality]
        public bool IsEmpty => IP == null && Mask == null && Port == null;

        public RuleItem<IPAddress> IP { get; set; }

        public IPAddress Mask { get; set; }

        public PortRuleItem Port { get; set; }

        internal BaseNetworkRuleItem()
        {

        }

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


        public static BaseNetworkRuleItem Parse(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentNullException(nameof(ip));

            BaseNetworkRuleItem bnri = new BaseNetworkRuleItem();
            bnri.IP = new RuleItem<IPAddress>();
            if (ip[0] == '!' || ip[0] == '！')
                bnri.IP.Invert = true;
            int index = ip.IndexOf('/');

            if (index > 0)
                bnri.Mask = IPAddress.Parse(ip.Substring(index + 1));

            ReadOnlySpan<char> ipAddress = ip.AsSpan();
            if (bnri.IP.Invert)
                ipAddress = ipAddress.Slice(1);
            if (index > 0)
                ipAddress = ipAddress.Slice(0, ipAddress.Length - index);
            bnri.IP.Value = IPAddress.Parse(ipAddress);
            return bnri;
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

        public void WriteXml(XmlWriter writer, string key)
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (IP != null)
                sb.Append(IP.ToString());
            if (Port != null)
                sb.Append(IP != null ? ":" : "Port=").Append(Port);
            if (Mask != null)
                sb.Append('/').Append(Mask.ToString());
            return sb.ToString();
        }

        public static implicit operator BaseNetworkRuleItem(RuleItem<IPAddress> value) => new BaseNetworkRuleItem(value);

        public static implicit operator BaseNetworkRuleItem(IPAddress value) => new BaseNetworkRuleItem(value);

        public static implicit operator BaseNetworkRuleItem(PortRuleItem value) => new BaseNetworkRuleItem(value);

        public static implicit operator BaseNetworkRuleItem(int value) => new BaseNetworkRuleItem(new PortRuleItem(new PortRange((ushort)value)));

        public static implicit operator BaseNetworkRuleItem(ushort value) => new BaseNetworkRuleItem(new PortRuleItem(new PortRange(value)));
    }
}
