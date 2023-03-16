using System.Xml;
using Firewall.Tables;
using Firewall.Rules;

namespace Firewall.Chains
{
    public class HandshakeChain : Chain
    {
        public Table<HandshakeRule> FilterTable { get; set; }

        public HandshakeChain()
        {
            FilterTable = new();
        }
        
        internal HandshakeChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new HandshakeRule(r), nameof(FilterTable));
        }

        protected override void WriteTables(XmlWriter writer)
        {
            FilterTable.Write(writer);
        }
    }
}
