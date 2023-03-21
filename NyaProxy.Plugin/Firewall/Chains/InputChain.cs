using System.Xml;
using Firewall.Tables;
using Firewall.Rules;
using System.Text;

namespace Firewall.Chains
{
    public class InputChain : FilterChain<PacketRule>
    {
        public InputChain()
        {
        }

        internal InputChain(XmlReader reader) : base(reader)
        {
        }
    }
}