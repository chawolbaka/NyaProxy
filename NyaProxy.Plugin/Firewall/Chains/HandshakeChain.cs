using System.Xml;
using NyaFirewall.Rules;

namespace NyaFirewall.Chains
{
    public class HandshakeChain : FilterChain<HandshakeRule>
    {
        protected override string CommandName => "handshake";

        public override string Description => "客户端发出握手请求";

        public HandshakeChain()
        {
        }
    }
}
