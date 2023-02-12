using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using NyaProxy.API;
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

        public async Task<CommandManager> RunAsync(string commandName, ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            try
            {
                await RegisteredCommands[commandName].ExecuteAsync(args, helper);
            }
            catch (CommandLeastRequiredException clre)
            {
                NyaProxy.Logger.UnpreformatColorfully(i18n.Error.CommandLeastRequired.Replace("{CommandName}", clre.Command, "{MinimumArgs}", clre.MinimumArgs));
            }
            catch (CommandNotFoundException cnfe)
            {
                NyaProxy.Logger.UnpreformatColorfully(i18n.Error.CommandNotFound.Replace("{CommandName}", cnfe.Command));
            }
            return this;
        }


    }
}
