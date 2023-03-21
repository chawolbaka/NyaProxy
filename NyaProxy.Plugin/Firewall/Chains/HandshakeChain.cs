using System.Xml;
using Firewall.Rules;

namespace Firewall.Chains
{
    public class HandshakeChain : FilterChain<HandshakeRule>
    {
        public HandshakeChain()
        {
        }

        internal HandshakeChain(XmlReader reader) : base(reader)
        {
        }
    }
}
