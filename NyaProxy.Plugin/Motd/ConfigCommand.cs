﻿using System;
using System.Net;
using NyaProxy.API.Command;
using MinecraftProtocol.Utils;

namespace Motd
{
    public class ConfigCommand : Command
    {
        public override string Name => "config";

        public override string Help => @"Usage: motd config [optine]
    reload <host>
    generate  [dest] <host>
";

        public override int MinimumArgs => 1;

        private static readonly IEnumerable<string> _firstTabList = new List<string>() { "reload", "generate" };
        private Action _reload;

        public ConfigCommand(Action reload)
        {
            _reload = reload;
        }

        public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            switch (args.Span[0])
            {
                case "reload": _reload(); break;
                case "generate":
                    if (args.Length < 2)
                        throw new CommandLeastRequiredException("generate", 1); //因为不是根命令所以这边需要+1
                    IPEndPoint endPoint = await NetworkUtils.GetIPEndPointAsync(args.Span[1]);
                    ServerListPing slp = new ServerListPing(endPoint);
                    MotdConfig config = new MotdConfig();
                    config.PingReply = await slp.SendAsync();
                    config.Host = args.Length > 2 ? args.Span[2] : endPoint.Address.ToString();

                    string fileName = $"{config.Host}.{MotdPlugin.CurrentInstance.Helper.Config.DefaultFileType}";
                    await MotdPlugin.CurrentInstance.Helper.Config.SaveAsync(MotdPlugin.CurrentInstance.Helper.Config.Register(config, Path.Combine("Hosts", fileName)));
                    helper.Logger.Unpreformat("§aGenerate success.");
                    break;
                default: helper.Logger.Unpreformat($"Unknow operate {args.Span[0]}"); break;
            }

        }

        public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
        {
            if (args.Length == 0)
                return _firstTabList;
            else
                return Enumerable.Empty<string>();
        }

    }

}