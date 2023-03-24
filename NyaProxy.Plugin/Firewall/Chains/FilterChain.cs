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


        internal virtual C ReadFromXml<C>(XmlReader reader) where C : FilterChain<T>
        {
            FilterTable = new(reader, (r) => new T().ReadFromXml<T>(r), nameof(FilterTable));
            return (C)this;
        }


        public override Command GetCommand()
        {
            return new ChainCommnad(this);
        }

        internal protected override void WriteTables(XmlWriter writer)
        {
            FilterTable.WriteXml(writer);
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
            
            public override string Description { get; }

            private FilterChain<T> _filterChain { get; }

            public ChainCommnad(FilterChain<T> filterChain)
            {
                Name = filterChain.GetType().Name.ToLower().Replace("chain", "");
                Description = filterChain.Description;
                _filterChain = filterChain;
                RegisterChild(new TableCommand<T>("filter", _filterChain.FilterTable));
                RegisterChild(new SimpleCommand("print", async (args, helper) =>
                {
                    string table = _filterChain.FilterTable.ToTable();
                    if (!string.IsNullOrWhiteSpace(table))
                        helper.Logger.Unpreformat(table);
                    else
                        helper.Logger.Unpreformat("Empty.");
                }));
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
