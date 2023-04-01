using System.Xml;
using NyaFirewall.Tables;
using NyaFirewall.Rules;
using System.Text;

namespace NyaFirewall.Chains
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