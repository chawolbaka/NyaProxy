using MinecraftProtocol.DataType;
using MinecraftProtocol.Utils;
using NyaProxy.API;
using System.Net;
using System.IO;

namespace Motd
{
    public class ConfigCommand : Command
    {
        public override string Name => "config";

        public override string Usage => "";

        public override string Description => "";

        private static readonly IEnumerable<string> _firstTabList = new List<string>() { "reload", "generate" };
        private Action Reload;
        
        public ConfigCommand(Action reload)
        {
            Reload = reload;
        }

        public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length is 1 or 2)
            {
                switch (args.Span[0])
                {
                    case "reload": Reload(); break;
                    case "generate":
                        if (args.Length < 2)
                            throw new CommandLeastRequiredException(Name, 3); //因为不是根命令所以这边需要+1
                        IPEndPoint endPoint = await NetworkUtils.GetIPEndPointAsync(args.Span[1]);
                        ServerListPing slp = new ServerListPing(endPoint);
                        MotdConfig config = new MotdConfig();
                        config.PingReply = await slp.SendAsync();
                        config.Host = endPoint.Address.ToString();
                        string fileName = Path.Combine("Hosts", $"{config.Host}.{MotdPlugin.CurrentInstance.Helper.Config.DefaultFileType}");
                        await MotdPlugin.CurrentInstance.Helper.Config.SaveAsync(MotdPlugin.CurrentInstance.Helper.Config.Register(config, fileName));

                        break;
                    default: helper.Logger.Unpreformat($"Unknow operate {args.Span[0]}"); break;
                }
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