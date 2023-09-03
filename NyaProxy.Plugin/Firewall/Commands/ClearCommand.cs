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
                helper.Logger.Unpreformat("§aClear success.");
            }
            catch (Exception e)
            {
                if (e is CommandException)
                    throw;

                helper.Logger.Exception(e);
                helper.Logger.Unpreformat("§cClear failed.");
                return false;
            }

            return true;
        }

    }
}
