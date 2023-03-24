using System.Xml;
using Firewall.Rules;

namespace Firewall.Chains
{
    public class HandshakeChain : FilterChain<HandshakeRule>
    {
        public override string Description => "客户端发出握手请求";

        public HandshakeChain()
        {
        }

        internal HandshakeChain(XmlReader reader) : base(reader)
        {
        }
    }
}
