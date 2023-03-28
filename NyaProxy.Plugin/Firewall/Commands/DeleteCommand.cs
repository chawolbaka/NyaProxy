using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Commands
{
    public class DeleteCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "delete";

        public virtual Table<T> Table { get; }

        private RuleCommandParser<T> _parser = new RuleCommandParser<T>();

        public DeleteCommand(Table<T> table)
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

                if (Table.Rules.Remove(_parser.Rule))
                    helper.Logger.Unpreformat("§aDelete success.");
                else
                    helper.Logger.Unpreformat("§cDelete failed.");
            }
            catch (Exception e)
            {
                helper.Logger.Exception(e);
                helper.Logger.Unpreformat("§cDelete failed.");
            }
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return _parser.GetTabCompletions(args);
        }
    }
}
