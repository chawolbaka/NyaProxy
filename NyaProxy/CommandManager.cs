using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.Extension;

namespace NyaProxy
{
    public class CommandManager
    {
        public SortedDictionary<string, (string Source, Command Commnad)> CommandDictionary { get; }
        public CommandManager()
        {
            CommandDictionary = new ();
        }

        public CommandManager Register(string source, Command command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (!CommandDictionary.ContainsKey(command.Name))
                CommandDictionary.Add(command.Name, (source,command));
            else
                NyaProxy.Logger.Error(i18n.Error.CommandRegistered.Replace("{CommandName}", command.Name));

            return this;
        }
        public async Task<CommandManager> RunAsync(string commandName, string[] args, ICommandHelper helper)
        {
            if (!CommandDictionary.ContainsKey(commandName))
            {
                NyaProxy.Logger.UnpreformatColorfully(i18n.Error.CommandNotFound.Replace("{CommandName}", commandName));
            }
            else if (CommandDictionary[commandName].Commnad.MinimumArgs > args.Length)
            {
                NyaProxy.Logger.UnpreformatColorfully(i18n.Error.CommandLeastRequired.Replace("{CommandName}", commandName, "{MinimumArgs}", CommandDictionary[commandName].Commnad.MinimumArgs));
            }
            else
            {
                await CommandDictionary[commandName].Commnad.ExecuteAsync(args, helper);
            }
            return this;
        }
    }
}
