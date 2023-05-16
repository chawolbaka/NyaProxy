using NyaProxy.API.Command;
using StringTable;

namespace Analysis
{
    public class PlayCommand : Command
    {
        public override string Name => "play";
        
        public override int MinimumArgs => 0;

        public virtual bool HideTime { get; set; }
        
        public PlayCommand()
        {
            AddOption(new Option("--show-time", 0, (command, e) => HideTime = false));
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            HideTime = true;
      
            if(await base.ExecuteAsync(args, helper))
            {
                if (AnalysisPlgin.Sessions.Count > 0)
                {
                    StringTableBuilder table = new StringTableBuilder();
                    table.AddColumn("Id", "Host", "Player", "Client", "Server", "Transferred");
                    if (!HideTime)
                        table.AddColumn("Connect", "Handshake", "LoginStart", "LoginSuccess", "Disconnect");
                    foreach (var session in AnalysisPlgin.Sessions.Values)
                        table.AddRow(session.ToRow(HideTime));

                    helper.Logger.Unpreformat(table.Export());
                }
                else
                {
                    helper.Logger.Unpreformat("当前无任何统计数据");
                }
            }
            return true;
        }
    }
}