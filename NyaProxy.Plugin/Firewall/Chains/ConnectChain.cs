using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;

namespace Firewall.Chains
{
    public class ConnectChain : Chain
    {
        public override bool IsEmpty => FilterTable == null || FilterTable.IsEmpty;

        public Table<Rule> FilterTable { get; set; }

        public ConnectChain()
        {
            FilterTable = new();
        }

        internal ConnectChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new Rule(r), nameof(FilterTable));
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
