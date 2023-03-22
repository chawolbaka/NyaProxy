using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Commands
{
    public class InsertCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "Insert";

        public virtual Table<T> Table { get; }

        private RuleCommandParse<T> _parse = new RuleCommandParse<T>();

        public InsertCommand(Table<T> table)
        {
            Table = table;
        }

        public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length == 0)
                return;

            try
            {
                _parse.Rule = new T();
                await _parse.ExecuteAsync(args, helper);
                Table.Rules.AddFirst(_parse.Rule);
                helper.Logger.Unpreformat("§aInsert success.");
            }
            catch (Exception e)
            {
                helper.Logger.Exception(e);
                helper.Logger.Unpreformat("§cInsert failed.");
            }
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return _parse.GetTabCompletions(args);
        }
    }
}
