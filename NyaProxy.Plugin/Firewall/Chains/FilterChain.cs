using System.Xml;
using Firewall.Tables;
using Firewall.Rules;
using System.Text;
using NyaProxy.API.Command;
using Firewall.Commands;

namespace Firewall.Chains
{
    public abstract class FilterChain<T> : Chain where T : Rule, new()
    {
        public override bool IsEmpty => FilterTable == null || FilterTable.IsEmpty;

        public Table<T> FilterTable { get; set; }

        public FilterChain()
        {
            FilterTable = new();
        }

        internal FilterChain(XmlReader reader)
        {
            FilterTable = new(reader, (r) => new T().Read<T>(r), nameof(FilterTable));
        }

        public override Command GetCommand()
        {
            return new ChainCommnad(this);
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

        private class ChainCommnad : Command
        {
            public override string Name { get; }
            public FilterChain<T> FilterChain { get; }

            public ChainCommnad(FilterChain<T> filterChain)
            {
                Name = filterChain.GetType().Name;
                FilterChain = filterChain;
                RegisterChild(new TableCommand<T>(nameof(FilterTable), FilterChain.FilterTable));
            }

            public override Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
            {
                return base.ExecuteChildrenAsync(args, helper);
            }

            public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
            {
                return base.GetChildrenTabCompletions(args);
            }
        }
    }
}
