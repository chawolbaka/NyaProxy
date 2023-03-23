using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Commands
{
    public class InsertCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "Insert";

        public virtual Table<T> Table { get; }

        private RuleCommandParser<T> _parser = new RuleCommandParser<T>();

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
                _parser.Rule = null;
                await _parser.ExecuteAsync(args, helper);
                if (_parser.Rule == null)
                    return;
                Table.Rules.AddFirst(_parser.Rule);
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
            return _parser.GetTabCompletions(args);
        }
    }
}
