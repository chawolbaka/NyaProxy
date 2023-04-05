using NyaFirewall.Rules;
using NyaFirewall.Tables;
using NyaProxy.API.Command;

namespace NyaFirewall.Commands
{
    public class TableCommand<T> : Command where T : Rule, new()
    {
        public override string Name { get; }

        public TableCommand(string name, Table<T> table)
        {
            Name = name;
            RegisterChild(new AddCommand<T>(table));
            RegisterChild(new InsertCommand<T>(table));
            RegisterChild(new DeleteCommand<T>(table));
            RegisterChild(new ClearCommand<T>(table));
        }

        public override Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            return base.ExecuteChildrenAsync(args, helper);
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return base.GetChildrenTabCompletions(args);
        }
    }
}
