using Firewall.Rules;
using Firewall.Tables;
using NyaProxy.API.Command;

namespace Firewall.Commands
{
    public class DeleteCommand<T> : Command where T : Rule, new()
    {
        public override string Name => "Delete";

        public virtual Table<T> Table { get; }

        private RuleCommandParse<T> _parse = new RuleCommandParse<T>();

        public DeleteCommand(Table<T> table)
        {
            Table = table;
        }

        public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            try
            {
                await _parse.ExecuteAsync(args, helper);
                if (Table.Rules.Remove(_parse.Rule))
                    helper.Logger.Unpreformat("§aDelete success.");
                else
                    helper.Logger.Unpreformat("§aDelete failed.");
            }
            catch (Exception e)
            {
                helper.Logger.Exception(e);
                helper.Logger.Unpreformat("§cDelete failed.");
            }
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return _parse.GetTabCompletions(args);
        }
    }
}
