using System.Xml;
using Firewall.Rules;
using Firewall.Tables;

namespace Firewall.Chains
{
    public class OutputChain : Chain
    {
        public Table<PacketRule> FilterTable { get; set; }

        public OutputChain()
        {
            FilterTable = new();
        }

        internal OutputChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new PacketRule(r), nameof(FilterTable));
        }

        protected override void WriteTables(XmlWriter writer)
        {
            FilterTable.Write(writer);
        }

    }
}
