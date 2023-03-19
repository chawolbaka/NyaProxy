using System;
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

        private string _host;
        public ConfigCommand(Action reload)
        {
            AddArgument(new Argument("reload", async (arg, helper) => reload()));
            AddOption(new Option("host", async (option, helper) => _host = option.Value));
            AddOption(new Option("generate", async (option, helper) => 
            {
                IPEndPoint endPoint = await NetworkUtils.GetIPEndPointAsync(option.Value);
                ServerListPing slp = new ServerListPing(endPoint);
                MotdConfig config = new MotdConfig();
                config.PingReply = await slp.SendAsync();
                config.Host = string.IsNullOrEmpty(_host) ? endPoint.Address.ToString() : _host;

                string fileName = $"{config.Host}.{MotdPlugin.CurrentInstance.Helper.Config.DefaultFileType}";
                await MotdPlugin.CurrentInstance.Helper.Config.SaveAsync(MotdPlugin.CurrentInstance.Helper.Config.Register(config, Path.Combine("Hosts", fileName)));
                helper.Logger.Unpreformat("§aGenerate success.");
            }));
        }

        public override Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            _host = null;
            return base.ExecuteAsync(args, helper);
        }

    }

}