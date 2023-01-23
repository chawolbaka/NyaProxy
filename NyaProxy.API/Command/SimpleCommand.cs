using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public class SimpleCommand : Command
    {
        public override string Name { get; }

        public override string Usage { get; }

        public override string Description { get; }

        private Func<string[], ICommandHelper, Task> _func;

        public SimpleCommand(string commandName, Func<string[], ICommandHelper, Task> executeAsync) : this(commandName, "", "", executeAsync) { }
        public SimpleCommand(string commandName, string commandUsage, Func<string[], ICommandHelper, Task> executeAsync) : this(commandName, commandUsage, "", executeAsync) { }
        public SimpleCommand(string commandName, string commandUsage, string commandDescription, Func<string[], ICommandHelper, Task> executeAsync)
        {
            Name = commandName ?? throw new ArgumentNullException(nameof(commandName));
            Usage = commandUsage ?? throw new ArgumentNullException(nameof(commandUsage));
            Description = commandDescription ?? throw new ArgumentNullException(nameof(commandDescription));
            _func = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        }

        public override Task ExecuteAsync(string[] args, ICommandHelper helper)
        {
            return _func(args, helper);
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return Enumerable.Empty<string>();
        }
    }
}
