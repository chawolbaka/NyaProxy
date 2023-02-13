﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NyaProxy.API;

namespace NyaProxy.Plugin
{
    internal partial class PluginHelper
    {
        internal class CommandContainer : ICommandContainer
        {
            public readonly IManifest Manifest;
            public readonly bool IsRoot;

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
                }
            }

            private class RootCommand : Command
            {
                public override string Name { get; }

                public override string Usage => "";

                public override string Description => "";

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