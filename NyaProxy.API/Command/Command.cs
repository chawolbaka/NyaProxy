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

                if (_children.Count > 0)
                {
                    sb.AppendLine("Commands:");
                    foreach (var command in _children.Values)
                    {
                        sb.Append("  ").Append(command.Name).Append("\t").AppendLine(command.Description);
                    }

                    sb.AppendLine().AppendLine("Options:");
                }
                else
                {
                    sb.AppendLine("Usage:");
                    sb.Append(' ').Append(Name).Append(" [options]").AppendLine().AppendLine();


                    if (_optionDictionray.Count > 0)
                    {
                        sb.AppendLine("Options:");
                        foreach (var option in _optionDictionray.Values)
                        {
                            sb.Append("  ").Append(option.Name);
                            if (option.Aliases != null)
                                sb.Append(", ").Append(string.Join(", ", option.Aliases));

                            sb.Append('\t').AppendLine(option.Description);
                        }
                    }
                }

                sb.Append("  ").Append(HELP_COMMAND).Append('\t').AppendLine("Show help and usage information");
                return sb.AppendLine().ToString();
            }
        }

        public virtual int MinimumArgs => _optionDictionray.Count > 0 ? 1 : 0;

        public Command Parent => _parent;
        private Command _parent;

        public ReadOnlyDictionary<string, Command> Children => new ReadOnlyDictionary<string, Command>(_children);
        private Dictionary<string, Command> _children = new Dictionary<string, Command>();
        private Dictionary<string, Option> _optionDictionray = new();

        private const string HELP_COMMAND = "--help";
        
        public void AddOption(Option option)
        {
            _optionDictionray.Add(option.Name, option);
            if (option.Aliases != null)
            {
                foreach (var alias in option.Aliases)
                {
                    _optionDictionray.Add(alias, option);
                }
            }
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="args">命令参数</param>
        /// <returns>命令是否还可以继续被执行</returns>
        public virtual async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (_children.Count > 0)
            {
                if (args.Length == 1 && args.Span[0] == HELP_COMMAND)
                {
                    helper.Logger.Unpreformat(Help);
                    return false;
                }
                else
                {
                    await ExecuteChildrenAsync(args, helper);
                    return true;
                }
            }
            else
            {
                if (args.ToArray().Any(s => s == HELP_COMMAND))
                {
                    helper.Logger.Unpreformat(Help);
                    return false;
                }
                else
                {
                    await ExecuteOptionAsync(args, helper);
                    return true;
                }
                    
            }
        }


        public virtual IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            if (_children.Count > 0)
                return GetChildrenTabCompletions(args);
            else
                return GetOptionTabCompletions(args);
        }

        public virtual async Task ExecuteOptionAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.IsEmpty)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                string currentArgument = args.Span[i];
                if (_optionDictionray.ContainsKey(currentArgument))
                {
                    var option = _optionDictionray[currentArgument];
                    if (i + option.MinimumArgs >= args.Length)
                        throw new MissingArgumentException(this, currentArgument);

                    CommandOptionEventArgs oea = new CommandOptionEventArgs(option, helper);
                    if (option.MinimumArgs > 0)
                    {
                        oea.Arguments = args.Slice(i + 1, option.MinimumArgs);
                        i += option.MinimumArgs;
                    }

                    if (option.Handler != null)
                        option.Handler(this, oea);
                    else if (option.AsyncHandler != null)
                        await option.AsyncHandler(this, oea);
                }
                else
                {
                    throw new UnrecognizedArgumentException(this, currentArgument);
                }
            }
        }

        public virtual IEnumerable<string> GetOptionTabCompletions(ReadOnlySpan<string> args)
        {
            if (_optionDictionray.Count == 0)
                return Enumerable.Empty<string>();

            if (args.Length == 0)
                return _optionDictionray.Keys;

            string[] arguments = args.ToArray();
            return _optionDictionray.Keys.Where(x => !arguments.Any(a => a == x));
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
    }
}
