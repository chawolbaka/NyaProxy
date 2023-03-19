using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace NyaProxy.API.Command
{
    public abstract class Command : IEquatable<Command>
    {
        public abstract string Name { get; }

        public abstract string Help { get; }

        public abstract Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper);

        public abstract IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args);

        public virtual int MinimumArgs => 0;

        public readonly Dictionary<string, Command> Children = new Dictionary<string, Command>();

        public Command Parent => _parent;
        private Command _parent;

        public bool Equals(Command other)
        {
            return other != null && other.Name == Name;
        }

        public override bool Equals(object obj)
        {
            return obj is Command command && command.Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public virtual void RegisterChild(Command command)
        {
            command._parent = this;
            if (Children.ContainsKey(command.Name))
                throw new CommandRegisteredException(command.Name);

            Children.Add(command.Name, command);
        }

        public virtual void UnregisterChild(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentNullException(commandName);

            Children.Remove(commandName);
        }

        public virtual async Task ExecuteChildrenAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (Children.Count == 0 || args.Length == 0)
                return;

            string commnad = args.Span[0];
            if (!Children.ContainsKey(commnad))
                throw new CommandNotFoundException(commnad);
            else if (Children[commnad].MinimumArgs > args.Length - 1)
                throw new CommandLeastRequiredException(commnad, Children[commnad].MinimumArgs);
            else
                await Children[commnad].ExecuteAsync(args.Slice(1), helper);
        }

        public virtual IEnumerable<string> GetChildrenTabCompletions(ReadOnlySpan<string> args)
        {
            if (Children.Count == 0)
                return Enumerable.Empty<string>();

            if (args.Length == 0)
                return Children.Keys;
            else if (Children.ContainsKey(args[0]))
                return Children[args[0]].GetTabCompletions(args.Slice(1));
            else
                return Enumerable.Empty<string>();
        }
    }
}
