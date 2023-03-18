using System.Xml;
using Firewall.Tables;
using Firewall.Rules;
using System.Text;

namespace Firewall.Chains
{
    public class InputChain : Chain
    {
        public override bool IsEmpty => FilterTable == null || FilterTable.IsEmpty;

        public Table<PacketRule> FilterTable { get; set; }

        public InputChain()
        {
            FilterTable = new();
        }

        internal InputChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new PacketRule(r), nameof(FilterTable));
        }

        internal protected override void WriteTables(XmlWriter writer)
        {
            FilterTable.Write(writer);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.AppendLine($"{nameof(FilterTable)}({FilterTable.Rules.Count})");
            sb.Append(FilterTable.ToTable());
            return sb.ToString();
        }
    }
}
