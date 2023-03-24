using System.Xml;
using Firewall.Tables;
using Firewall.Rules;
using System.Text;

namespace Firewall.Chains
{
    public class ClientChain : FilterChain<PacketRule>
    {
        protected override string CommandName => "client";

        public override string Description => "客户端发出的数据包";

        public ClientChain()
        {
        }
    }
}