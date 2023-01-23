using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.Extension;

namespace NyaProxy
{
    public class CommandManager
    {
        public SortedDictionary<string, (string Source, Command Commnad)> RegisteredCommands { get; }

        public CommandManager()
        {
            RegisteredCommands = new ();
        }

        public CommandManager Register(string source, Command command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (!RegisteredCommands.ContainsKey(command.Name))
                RegisteredCommands.Add(command.Name, (source,command));
            else
                NyaProxy.Logger.Error(i18n.Error.CommandRegistered.Replace("{CommandName}", command.Name));

            return this;
        }

        public bool Unregister(string commandName)
        {
            return RegisteredCommands.Remove(commandName);
        }

        public async Task<CommandManager> RunAsync(string commandName, string[] args, ICommandHelper helper)
        {
            if (!RegisteredCommands.ContainsKey(commandName))
            {
                NyaProxy.Logger.UnpreformatColorfully(i18n.Error.CommandNotFound.Replace("{CommandName}", commandName));
            }
            else if (RegisteredCommands[commandName].Commnad.MinimumArgs > args.Length)
            {
                NyaProxy.Logger.UnpreformatColorfully(i18n.Error.CommandLeastRequired.Replace("{CommandName}", commandName, "{MinimumArgs}", RegisteredCommands[commandName].Commnad.MinimumArgs));
            }
            else
            {
                await RegisteredCommands[commandName].Commnad.ExecuteAsync(args, helper);
            }
            return this;
        }
    }
}
