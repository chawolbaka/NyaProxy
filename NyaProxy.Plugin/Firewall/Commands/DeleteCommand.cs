using NyaFirewall.Rules;
using NyaFirewall.Tables;
using NyaProxy.API.Command;

namespace NyaFirewall.Commands
{
    public class DeleteCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "delete";

        public virtual Table<T> Table { get; }

        private RuleCommandParser<T> _parser = new RuleCommandParser<T>("delete");

        public DeleteCommand(Table<T> table)
        {
            Table = table;
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length == 0)
                return false;

            try
            {
                _parser.Rule = null;
                await _parser.ExecuteAsync(args, helper);
                if (_parser.Rule == null)
                    return true;

                if (Table.Rules.Remove(_parser.Rule))
                    helper.Logger.Unpreformat("§aDelete success.");
                else
                    helper.Logger.Unpreformat("§cDelete failed.");
            }
            catch (Exception e)
            {
                if (e is CommandException)
                    throw;

                helper.Logger.Exception(e);
                helper.Logger.Unpreformat("§cDelete failed.");
            }
            return false;
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return _parser.GetTabCompletions(args);
        }
    }
}
