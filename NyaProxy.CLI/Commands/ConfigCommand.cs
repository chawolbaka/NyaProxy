using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NyaProxy.API.Command;
using NyaProxy.Configs;

namespace NyaProxy.CLI.Commands
{
    public sealed class ConfigCommand : Command
    {
        public override string Name => "config";

        private static IEnumerable<string> _tabCompletions = new List<string>() { "reload", "save" };

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length > 0)
            {
                switch (args.Span[0])
                {
                    case "reload": 
                        NyaProxy.ReloadConfig();
                        NyaProxy.ReloadHosts();
                        NyaProxy.RebindSockets();
                        foreach (var server in NyaProxy.Hosts)
                        {
                            helper.Logger.LogInformation($"{server.Value.Name} -> [{string.Join(", ", server.Value.ServerEndPoints.Select(x => x.ToString()))}]");
                        } break;
                    case "save": var w = new TomlConfigWriter(); NyaProxy.Config.Write(w); w.Save("config.toml"); break;
                    default: throw new UnrecognizedArgumentException(this, args.Span[0]);
                }
            }
            return true;
        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            return _tabCompletions;
        }
    }
}
