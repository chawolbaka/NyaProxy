using Microsoft.Extensions.Logging;
using NyaFirewall.Rules;
using NyaFirewall.Tables;
using NyaProxy.API.Command;

namespace NyaFirewall.Commands
{
    public class ClearCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "clear";

        public virtual Table<T> Table { get; }

        public ClearCommand(Table<T> table)
        {
            Table = table;
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length == 0)
                throw new CommandLeastRequiredException(this);

            try
            {
                Table.Rules.Clear();
                helper.Logger.LogInformation("§aClear success.");
            }
            catch (Exception e)
            {
                if (e is CommandException)
                    throw;

                helper.Logger.LogMultiLineError("§cClear failed.", e);
                return false;
            }

            return true;
        }

    }
}
