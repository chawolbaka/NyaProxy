using Microsoft.Extensions.Logging;
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
                throw new CommandLeastRequiredException(this);

            try
            {
                await _parser.ExecuteAsync(args, helper);
                
                if (Table.Rules.Remove(_parser.Rule))
                    helper.Logger.LogInformation("§aDelete success.");
                else
                    helper.Logger.LogError("§cDelete failed.");
            }
            catch (Exception e)
            {
                if (e is CommandException)
                    throw;

                helper.Logger.LogMultiLineError("§cDelete failed.", e);
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
