using NyaProxy.API.Command;

namespace Analysis.Commands
{
    public class OverviewCommand : Command
    {
        public override string Name => "overview";

        public OverviewCommand()
        {
            RegisterChild(new ServerListPingCommand());
            RegisterChild(new PlayCommand());
            RegisterChild(new PacketCommand());
        }

    }
}