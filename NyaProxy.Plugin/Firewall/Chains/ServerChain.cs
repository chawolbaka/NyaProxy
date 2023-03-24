using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Chains
{
    public class ServerChain : FilterChain<PacketRule>
    {
        public override string Description => "服务端发出的数据包";

        public ServerChain()
        {
        }

        internal ServerChain(XmlReader reader) : base(reader)
        {
        }
    }
}
