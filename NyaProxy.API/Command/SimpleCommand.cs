using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Command
{
    public sealed class SimpleCommand : Command
    {
        public override string Name { get; }

        public override string Help { get; }

        public override int MinimumArgs => 0;

        private Func<ReadOnlyMemory<string>, ICommandHelper, Task> _func;

        public SimpleCommand(string commandName, Func<ReadOnlyMemory<string>, ICommandHelper, Task> executeAsync) : this(commandName, "", executeAsync) { }
        public SimpleCommand(string commandName, string commandHelp, Func<ReadOnlyMemory<string>, ICommandHelper, Task> executeAsync)
        {
            Name = commandName ?? throw new ArgumentNullException(nameof(commandName));
            Help = commandHelp ?? throw new ArgumentNullException(nameof(commandHelp));
            _func = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            await _func(args, helper);
            return false; 
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return Enumerable.Empty<string>();
        }
    }
}
