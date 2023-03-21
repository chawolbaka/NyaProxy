using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Chains
{
    public class ConnectChain : FilterChain<Rule>
    {
        public ConnectChain()
        {
        }

        internal ConnectChain(XmlReader reader) : base(reader)
        {
        }
    }
}
