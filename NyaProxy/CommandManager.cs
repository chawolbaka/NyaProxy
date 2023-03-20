using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NyaProxy.API.Command;
using NyaProxy.Extension;

namespace NyaProxy
{
    public class CommandManager
    {
        public Dictionary<string, Command> RegisteredCommands { get; }

        public CommandManager()
        {
            RegisteredCommands = new ();
        }

        public CommandManager Register(Command command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (!RegisteredCommands.ContainsKey(command.Name))
                RegisteredCommands.Add(command.Name, command);
            else
                NyaProxy.Logger.Error(i18n.Error.CommandRegistered.Replace("{CommandName}", command.Name));


            return this;
        }


        public CommandManager Unregister(Command command)
        {
            return Unregister(command.Name);
        }

        public CommandManager Unregister(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentNullException(nameof(commandName));

            RegisteredCommands.Remove(commandName);
            return this;
        }

        public async Task<CommandManager> RunAsync(string commandName, ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            try
            {
                if (!RegisteredCommands.ContainsKey(commandName))
                    throw new CommandNotFoundException(commandName);

                await RegisteredCommands[commandName].ExecuteAsync(args, helper);
            }
            catch (MissingArgumentException mcae)
            {
                NyaProxy.Logger.Unpreformat(i18n.Error.MissingArgument.Replace("{CommandName}", mcae.CommandName, "{Argument}", mcae.Argument));
            }
            catch (UnrecognizedArgumentException ucae)
            {
                NyaProxy.Logger.Unpreformat(i18n.Error.UnrecognizedCommandArgument.Replace("{CommandName}", ucae.CommandName, "{Argument}", ucae.Argument));
            }
            catch (CommandLeastRequiredException clre)
            {
                NyaProxy.Logger.Unpreformat(i18n.Error.CommandLeastRequired.Replace("{CommandName}", clre.Command, "{MinimumArgs}", clre.MinimumArgs));
                NyaProxy.Logger.Unpreformat(clre.Command.Help);
            }
            catch (CommandNotFoundException cnfe)
            {
                NyaProxy.Logger.Unpreformat(i18n.Error.CommandNotFound.Replace("{CommandName}", cnfe.CommandName));
            }
            return this;
        }


    }
}
