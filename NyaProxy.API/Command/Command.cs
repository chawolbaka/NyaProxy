using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace NyaProxy.API
{
    public abstract class Command
    {
        public abstract string Name { get; }
        public abstract string Usage { get; }
        public abstract string Description { get; }
        public abstract Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper);
        public abstract IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args);
        public virtual int MinimumArgs => 0;
        public virtual string Helper => Usage;


        public readonly Dictionary<string, Command> Children = new Dictionary<string, Command>();

        public Command Parent => _parent;
        private Command _parent;
        
        public virtual void RegisterChild(Command command)
        {
            command._parent = this;
            if(Children.ContainsKey(command.Name))
                throw new CommandRegisteredException(command.Name);

            Children.Add(command.Name, command);
        }

        public virtual async Task ExecuteChildrenAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (Children.Count == 0)
                return;

            string commnad = args.Span[0];
            if (!Children.ContainsKey(commnad))
                throw new CommandNotFoundException(commnad);
            else if (Children[commnad].MinimumArgs > args.Length)
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
