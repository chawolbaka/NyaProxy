using System.Xml;
using Firewall.Rules;
using Firewall.Tables;

namespace Firewall.Chains
{
    public class ConnectChain : Chain
    {
        public Table<Rule> FilterTable { get; set; }

        public ConnectChain()
        {
            FilterTable = new();
        }

        internal ConnectChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new Rule(r), nameof(FilterTable));
        }

        protected override void WriteTables(XmlWriter writer)
        {
            FilterTable.Write(writer);
        }
    }
}
