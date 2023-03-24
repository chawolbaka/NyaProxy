using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace NyaProxy.API.Command
{
    public abstract class Command : IEquatable<Command>
    {
        public abstract string Name { get; }

        public virtual string Description { get; }

        public virtual string Help
        {
            get
            {
                StringBuilder sb = new StringBuilder("§7");
                if (!string.IsNullOrWhiteSpace(Description))
                    sb.Append("Description: ").AppendLine(Description).AppendLine().AppendLine();

                sb.AppendLine("Usage:");
                sb.Append(' ').Append(Name).Append(' ');
                if(_arguments.Count>0)
                {
                    foreach (var argument in _arguments)
                    {

                        sb.Append('<').Append(argument.Name).Append("> ");
                    }
                }
                sb.AppendLine("[options]").AppendLine();
                
                if (_arguments.Count > 0)
                {
                    sb.AppendLine("Arguments:");
                    foreach (var argument in _arguments)
                    {
                        sb.Append("  ").Append(argument.Name);
                        if (argument.Aliases != null)
                            sb.Append(", ").Append(string.Join(", ", argument.Aliases));

                        sb.Append("\t\t").AppendLine(argument.Description);
                    }
                    sb.AppendLine();
                }

                if(_options.Count > 0)
                {
                    sb.AppendLine("Options:");
                    foreach (var option in _options)
                    {
                        sb.Append("  ").Append(option.Name);
                        if (option.Aliases != null)
                            sb.Append(", ").Append(string.Join(", ", option.Aliases));

                        sb.Append(" <value>\t\t").AppendLine(option.Description);
                    }
                    sb.AppendLine();
                }
                return sb.ToString();
            }
        }

        public virtual int MinimumArgs => _aoDictionray.Count > 0 ? 1 : 0;

        public Command Parent => _parent;
        private Command _parent;

        public ReadOnlyDictionary<string, Command> Children => new ReadOnlyDictionary<string, Command>(_children);
        private Dictionary<string, Command> _children = new Dictionary<string, Command>();
        
        private Dictionary<string, object> _aoDictionray = new();
        private List<Argument> _arguments = new();
        private List<Option> _options = new();

        public Command()
        {
            AddArgument(new Argument("-h", "Show help and usage information", (command, arg, helper) => helper.Logger.Unpreformat(command.Help), "--help"));
        }

        public void AddArgument(Argument argument)
        {
            _aoDictionray.Add(argument.Name, argument);
            if (argument.Aliases != null)
            {
                foreach (var alias in argument.Aliases)
                {
                    _aoDictionray.Add(alias, argument);
                }
            }
            _arguments.Add(argument);
        }

        public void AddOption(Option option)
        {
            _aoDictionray.Add(option.Name, option);
            if (option.Aliases != null)
            {
                foreach (var alias in option.Aliases)
                {
                    _aoDictionray.Add(alias, option);
                }
            }

            _options.Add(option);
        }

        public virtual async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.IsEmpty)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                string currentArgument = args.Span[i];
                if (_aoDictionray.ContainsKey(currentArgument))
                {
                    if (_aoDictionray[currentArgument] is Argument argument)
                    {
                        if (argument.Handler != null)
                            argument.Handler(this, argument, helper);
                        else
                            await argument.AsyncHandler(this, argument, helper);
                    }
                    else if (_aoDictionray[currentArgument] is Option option)
                    {
                        if (i + 1 >= args.Length)
                            throw new MissingArgumentException(this, currentArgument);

                        option.Value = args.Span[++i];

                        if (option.Handler != null)
                            option.Handler(this, option, helper);
                        else
                            await option.AsyncHandler(this, option, helper);                       
                    }
                }
                else
                {
                    throw new UnrecognizedArgumentException(this, currentArgument);
                }
            }
        }

        public virtual IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            if (_aoDictionray.Count == 0 || (args.Length > 0 && _aoDictionray.TryGetValue(args[args.Length - 1], out var x) && x is Option))
                return Enumerable.Empty<string>();

            if (args.Length == 0)
                return _aoDictionray.Keys;

            string[] arguments = args.ToArray();
            return _aoDictionray.Keys.Where(x => !arguments.Any(a => a == x));
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
                throw new CommandLeastRequiredException(_children[commnad], _children[commnad].MinimumArgs);
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
