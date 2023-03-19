using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;

namespace NyaProxy.API.Command
{
    public abstract class Command : IEquatable<Command>
    {
        public abstract string Name { get; }

        public abstract string Help { get; }

        public virtual int MinimumArgs => _arguments.Count > 0 ? 1 : 0;

        public Command Parent => _parent;
        private Command _parent;

        public ReadOnlyDictionary<string, Command> Children => new ReadOnlyDictionary<string, Command>(_children);
        private Dictionary<string, Command> _children = new Dictionary<string, Command>();
        private Dictionary<string, object> _arguments = new ();

        public void AddArgument(Argument argument)
        {
            _arguments.Add(argument.Name, argument);
            if (argument.Aliases == null)
                return;

            foreach (var aliase in argument.Aliases)
            {
                _arguments.Add(aliase, argument);
            }
        }

        public void AddOption(Option option)
        {
            _arguments.Add(option.Name, option);
            if (option.Aliases == null)
                return;

            foreach (var aliase in option.Aliases)
            {
                _arguments.Add(aliase, option);
            }
        }

        public virtual async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string currentArgument = args.Span[i];
                if (_arguments.ContainsKey(currentArgument))
                {
                    if (_arguments[currentArgument] is Argument argument)
                    {
                        await argument.Handler(argument, helper);
                    }
                    else if (_arguments[currentArgument] is Option option)
                    {
                        if (i + 1 >= args.Length)
                            throw new MissingArgumentException(Name, currentArgument);

                        option.Value = args.Span[++i];
                        await option.Handler(option, helper);
                    }
                }
                else
                {
                    throw new UnrecognizedArgumentException(Name, currentArgument);
                }
            }
        }

        public virtual IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            if (_arguments.Count == 0 || (args.Length > 0 && _arguments.TryGetValue(args[args.Length - 1], out var x) && x is Option))
                return Enumerable.Empty<string>();

            if (args.Length == 0)
                return _arguments.Keys;

            string[] arguments = args.ToArray();
            return _arguments.Keys.Where(x => !arguments.Any(a => a == x));
        }


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
            if (_children.ContainsKey(command.Name))
                throw new CommandRegisteredException(command.Name);

            _children.Add(command.Name, command);
        }

        public virtual void UnregisterChild(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentNullException(commandName);

            _children.Remove(commandName);
        }

        public virtual async Task ExecuteChildrenAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (_children.Count == 0 || args.Length == 0)
                return;

            string commnad = args.Span[0];
            if (!_children.ContainsKey(commnad))
                throw new CommandNotFoundException(commnad);
            else if (_children[commnad].MinimumArgs > args.Length - 1)
                throw new CommandLeastRequiredException(commnad, _children[commnad].MinimumArgs);
            else
                await _children[commnad].ExecuteAsync(args.Slice(1), helper);
        }

        public virtual IEnumerable<string> GetChildrenTabCompletions(ReadOnlySpan<string> args)
        {
            if (_children.Count == 0)
                return Enumerable.Empty<string>();

            if (args.Length == 0)
                return _children.Keys;
            else if (_children.ContainsKey(args[0]))
                return _children[args[0]].GetTabCompletions(args.Slice(1));
            else
                return Enumerable.Empty<string>();
        }
    }
}
