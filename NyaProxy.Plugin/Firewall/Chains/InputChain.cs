using System.Xml;
using Firewall.Tables;
using Firewall.Rules;

namespace Firewall.Chains
{
    public class InputChain : Chain
    {
        public Table<PacketRule> FilterTable { get; set; }

        public InputChain()
        {
            FilterTable = new();
        }

        internal InputChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new PacketRule(r), nameof(FilterTable));
        }

        protected override void WriteTables(XmlWriter writer)
        {
            FilterTable.Write(writer);
        }

    }
}
