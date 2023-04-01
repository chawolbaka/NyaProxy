using System.Text;
using System.Xml;
using NyaFirewall.Rules;
using NyaFirewall.Tables;
using NyaProxy.API.Command;

namespace NyaFirewall.Chains
{
    public class ServerChain : FilterChain<PacketRule>
    {
        protected override string CommandName => "server";

        public override string Description => "服务端发出的数据包";

        public ServerChain()
        {
        }
    }
}
