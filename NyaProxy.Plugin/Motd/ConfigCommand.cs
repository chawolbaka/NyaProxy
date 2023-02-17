using MinecraftProtocol.DataType;
using MinecraftProtocol.Utils;
using NyaProxy.API;

namespace Motd
{
    public class ConfigCommand : Command
    {
        public override string Name => "config";

        public override string Usage => "";

        public override string Description => "";

        private static readonly IEnumerable<string> _firstTabList = new List<string>() { "reload"};
        private Action Reload;
        
        public ConfigCommand(Action reload)
        {
            Reload = reload;
        }

        public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (args.Length == 1)
            {
                switch (args.Span[0])
                {
                    case "reload": Reload(); break;
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