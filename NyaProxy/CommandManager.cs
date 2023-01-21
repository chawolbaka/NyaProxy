using System;
using System.Collections.Generic;
using NyaProxy.API;


namespace NyaProxy
{
    public class CommandManager
    {
        public SortedDictionary<string,(string Source, Command Commnad)> CommandDictionary { get; }
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
        public CommandManager Run(string commandName, ReadOnlySpan<string> args)
        {
            //Unpreformat(i18n.Error.CommandLeastRequired.Replace("{CommandName}", commandName).Replace("MinimumArgs", CommandDictionary[commandName].Commnad.MinimumArgs.ToString()))
            //这块改成单纯的执行，错误提示往控制台内部移动
            //我可能需要套娃使用，还是别移动出去了
            if (!CommandDictionary.ContainsKey(commandName))
                NyaProxy.Logger.UnpreformatColorfully(i18n.Error.CommandNotFound.Replace("{CommandName}", commandName));
            else if (CommandDictionary[commandName].Commnad.MinimumArgs > args.Length)
                NyaProxy.Logger.Unpreformat(CommandDictionary[commandName].Commnad.Usage); 
            else
                CommandDictionary[commandName].Commnad.Execute(args);
            
            return this;
        }
    }
}
