using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Chains
{
    public class OutputChain : FilterChain<PacketRule>
    {
        public OutputChain()
        {
        }

        internal OutputChain(XmlReader reader) : base(reader)
        {
        }
    }
}
