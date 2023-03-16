using System.Xml;
using Firewall.Rules;
using Firewall.Tables;

namespace Firewall.Chains
{
    public class LoginChain : Chain
    {
        public Table<LoginRule> FilterTable { get; set; }

        public LoginChain()
        {
            FilterTable = new();
        }

        internal LoginChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new LoginRule(r), nameof(FilterTable));
        }

        protected override void WriteTables(XmlWriter writer)
        {
            FilterTable.Write(writer);
        }
    }
}
