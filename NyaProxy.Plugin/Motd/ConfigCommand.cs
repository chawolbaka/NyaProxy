using System;
using System.Net;
using NyaProxy.API.Command;
using MinecraftProtocol.Utils;

namespace Motd
{
    public class ConfigCommand : Command
    {
        public override string Name => "config";

        private string _host;
        private string _address;
        public ConfigCommand(Action reload) : base()
        {
            AddOption(new Option("reload",      (command, e) => reload()));
            AddOption(new Option("host",     1, (command, e) => _host    = e.Arguments.Span[0]));
            AddOption(new Option("generate", 1, (command, e) => _address = e.Arguments.Span[0]));
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            _host = null; _address = null;
            await base.ExecuteAsync(args, helper);
            if (_address != null)
            {
                IPEndPoint endPoint = await NetworkUtils.GetIPEndPointAsync(_address);
                ServerListPing slp = new ServerListPing(endPoint);
                MotdConfig config = new MotdConfig();
                config.PingReply = await slp.SendAsync();
                config.Host = string.IsNullOrEmpty(_host) ? endPoint.Address.ToString() : _host;

                string fileName = $"{config.Host}.{MotdPlugin.CurrentInstance.Helper.Config.DefaultFileType}";
                await MotdPlugin.CurrentInstance.Helper.Config.SaveAsync(MotdPlugin.CurrentInstance.Helper.Config.Register(config, Path.Combine("Hosts", fileName)));
                helper.Logger.Unpreformat("§aGenerate success.");
            }
            return true;
        }
    }
}