using System.Xml;
using NyaFirewall.Tables;
using NyaFirewall.Rules;
using System.Text;
using NyaProxy.API.Command;
using NyaFirewall.Commands;
using Microsoft.Extensions.Logging;

namespace NyaFirewall.Chains
{
    public abstract class FilterChain<T> : Chain where T : Rule, new()
    {
        public override bool IsEmpty => FilterTable == null || FilterTable.IsEmpty;

        protected abstract string CommandName { get; }

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
                Name = filterChain.CommandName;
                Description = filterChain.Description;
                _filterChain = filterChain;
                RegisterChild(new TableCommand<T>("filter", _filterChain.FilterTable));
                RegisterChild(new SimpleCommand("print", async (args, helper) =>
                {
                    string table = _filterChain.FilterTable.ToTable();
                    if (!string.IsNullOrWhiteSpace(table))
                        helper.Logger.LogMultiLineInformation($"{_filterChain.FilterTable.Count()} rules.", table);
                    else
                        helper.Logger.LogInformation("Empty table.");
                }));
            }
        }
    }
}
