using Microsoft.Extensions.Logging;
using NyaFirewall.Rules;
using NyaFirewall.Tables;
using NyaProxy.API.Command;

namespace NyaFirewall.Commands
{
    public class InsertCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "insert";

        public virtual Table<T> Table { get; }

        private RuleCommandParser<T> _parser = new RuleCommandParser<T>("insert");

        public InsertCommand(Table<T> table)
        {
            Table = table;
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length == 0)
                throw new CommandLeastRequiredException(this);

            try
            {
                await _parser.ExecuteAsync(args, helper);
                Table.Rules.AddFirst(_parser.Rule);
                helper.Logger.LogInformation("§aInsert success.");
            }
            catch (Exception e)
            {
                if (e is CommandException)
                    throw;

                helper.Logger.LogMultiLineError("§cInsert failed.", e);
                return false;
            }
            return true;
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return _parser.GetTabCompletions(args);
        }
    }
}
