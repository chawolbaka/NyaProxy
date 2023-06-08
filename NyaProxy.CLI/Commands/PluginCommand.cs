using ConsolePlus;
using NyaProxy.API.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.CLI.Commands
{
    public sealed class PluginCommand : Command
    {
        public override string Name => "plugin";

        public override string Help => "Usege: plugin [load/unload/reload] [PluginName]";

        public override int MinimumArgs => 2;

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            switch (args.Span[0])
            {
                case "load": await NyaProxy.Plugins.LoadAsync(args.Span[1]); break;
                case "unload": if (IsIdExists(args.Span[1])) await NyaProxy.Plugins[args.Span[1]].UnloadAsync(); break;
                case "reload": if (IsIdExists(args.Span[1])) await NyaProxy.Plugins[args.Span[1]].ReloadAsync(); break;
            }
            return false;

            bool IsIdExists(string id)
            {
                if (NyaProxy.Plugins.Contains(id))
                    return true;
                helper.Logger.Unpreformat($"§e插件{args.Span[1]}不存在");
                return false;
            }
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            if (args.Length == 2)
            {
                if (args[0] == "load")
                    return Directory.GetDirectories(Path.Combine(Environment.CurrentDirectory, "Plugins"));
                else
                    return NyaProxy.Plugins.Select(x => x.Plugin.Manifest.UniqueId);
            }
            else if (args.Length == 1)
                return new List<string>() { "load", "unload", "reload" };
            else
                return null;
        }
    }
}
