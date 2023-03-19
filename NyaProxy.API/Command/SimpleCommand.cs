using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Command
{
    public class SimpleCommand : Command
    {
        public override string Name { get; }

        public override string Help { get; }

        private Func<ReadOnlyMemory<string>, ICommandHelper, Task> _func;

        public SimpleCommand(string commandName, Func<ReadOnlyMemory<string>, ICommandHelper, Task> executeAsync) : this(commandName, "", executeAsync) { }
        public SimpleCommand(string commandName, string commandHelp, Func<ReadOnlyMemory<string>, ICommandHelper, Task> executeAsync)
        {
            Name = commandName ?? throw new ArgumentNullException(nameof(commandName));
            Help = commandHelp ?? throw new ArgumentNullException(nameof(commandHelp));
            _func = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        }

        public override Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            return _func(args, helper);
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return Enumerable.Empty<string>();
        }
    }
}
