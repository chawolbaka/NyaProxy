using System.Text;
using System.Xml;
using Firewall.Rules;
using Firewall.Tables;

namespace Firewall.Chains
{
    public class LoginChain : Chain
    {
        public override bool IsEmpty => FilterTable == null || FilterTable.IsEmpty;

        public Table<LoginRule> FilterTable { get; set; }

        public LoginChain()
        {
            FilterTable = new();
        }

        internal LoginChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new LoginRule(r), nameof(FilterTable));
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
