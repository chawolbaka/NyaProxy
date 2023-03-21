using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;

namespace Firewall.Chains
{
    public class LoginChain : FilterChain<LoginRule>
    {
        public LoginChain()
        {
        }

        internal LoginChain(XmlReader reader) : base(reader)
        {
        }
    }
}
