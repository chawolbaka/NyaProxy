using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Chains
{
    public class ServerChain : FilterChain<PacketRule>
    {
        public ServerChain()
        {
        }

        internal ServerChain(XmlReader reader) : base(reader)
        {
        }
    }
}
