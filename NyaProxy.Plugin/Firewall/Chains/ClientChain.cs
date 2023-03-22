using System.Xml;
using Firewall.Tables;
using Firewall.Rules;
using System.Text;

namespace Firewall.Chains
{
    public class ClientChain : FilterChain<PacketRule>
    {
        public ClientChain()
        {
        }

        internal ClientChain(XmlReader reader) : base(reader)
        {
        }
    }
}