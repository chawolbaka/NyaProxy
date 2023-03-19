using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NyaProxy.API.Config;
using NyaProxy.API;
using NyaProxy.API.Command;

namespace NyaProxy.Plugin
{
    internal partial class PluginHelper
    {
        internal class CommandContainer : ICommandContainer
        {
            public readonly IManifest Manifest;
            public readonly bool IsRoot;
            public readonly List<string> CommandList = new List<string>();

            public CommandContainer(IManifest manifest)
            {
                Manifest = manifest;
                IsRoot = manifest.CommandPrefixes == null || manifest.CommandPrefixes.Count == 0;
                if (!IsRoot)
                {
                    foreach (var prefix in Manifest.CommandPrefixes)
                    {
                        NyaProxy.CommandManager.Register(new RootCommand(prefix));
                    }
                }
            }

            public void Register(Command command)
            {
                if (!IsRoot)
                {
                    foreach (var prefix in Manifest.CommandPrefixes)
                    {
                        try
                        {
                            NyaProxy.CommandManager.RegisteredCommands[prefix].RegisterChild(command);
                        }
                        catch (CommandRegisteredException cre)
                        {
                            NyaProxy.Logger.Error(i18n.Error.CommandRegistered.Replace("{CommandName}", cre.Command));
                        }
                    }
                }
                else
                {
                    NyaProxy.CommandManager.Register(command);
                    CommandList.Add(command.Name);
                }
            }

            public void Unregister(string commandName)
            {
                if (!IsRoot)
                {
                    foreach (var prefix in Manifest.CommandPrefixes)
                    {
                        NyaProxy.CommandManager.RegisteredCommands[prefix].UnregisterChild(commandName);
                    }
                }
                else
                {
                    NyaProxy.CommandManager.Unregister(commandName);
                    CommandList.Remove(commandName);
                }
            }

            public void UnregisterAll()
            {
                if (!IsRoot)
                {
                    foreach (var prefix in Manifest.CommandPrefixes)
                    {
                        try
                        {
                            NyaProxy.CommandManager.Unregister(prefix);
                        }
                        catch (Exception e)
                        {
                            NyaProxy.Logger.Exception(e);
                        }
                    }
                }
                else
                {
                    foreach (var command in CommandList)
                    {
                        NyaProxy.CommandManager.Unregister(command);
                    }
                    CommandList.Clear();
                }
            }

            private class RootCommand : Command
            {
                public override string Name { get; }

                public override string Help => "";

                public RootCommand(string name)
                {
                    Name = name;
                }

                public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
                {
                    await ExecuteChildrenAsync(args, helper);
                }

                public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
                {
                    return GetChildrenTabCompletions(args);
                }
            }
        }

    }
}
