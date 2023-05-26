using NyaProxy.API;
using NyaProxy.API.Command;
using StringTable;

namespace Analysis.Commands
{
    public class PlayCommand : Command
    {
        public override string Name => "play";

        public override int MinimumArgs => 0;

        public virtual bool ShowShortTime { get; set; }

        public virtual bool ShowFullTime { get; set; }

        public PlayCommand()
        {
            AddOption(new Option("--show-time", 0, (command, e) => ShowShortTime = true));

            AddOption(new Option("--show-time-full", 0, (command, e) => ShowFullTime = true));
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            ShowShortTime = false;
            ShowFullTime = false;

            if (await base.ExecuteAsync(args, helper))
            {
                if (AnalysisData.Sessions.Count > 0)
                {
                    StringTableBuilder table = new StringTableBuilder();
                    table.AddColumn("Id", "Host", "Player", "Client", "Server", "Transferred");
                    if (ShowFullTime)
                        table.AddColumn("Connect", "Handshake", "LoginStart", "LoginSuccess", "Disconnect");
                    else if (ShowShortTime)
                        table.AddColumn("Connect", "Disconnect");


                    foreach (var session in AnalysisData.Sessions.Values)
                    {
                        List<object> row = new List<object>
                        { 
                            session.SessionId,
                            session.Host != null ? session.Host.Name : "",
                            session.Player != null ? session.Player.Name : "",
                            session.Source != null ? session.Source.ToString() : "",
                            session.Destination != null ? session.Destination.ToString() : "",
                            Utils.SizeSuffix(session.PacketAnalysis.Client.BytesTransferred+session.PacketAnalysis.Server.BytesTransferred)};

                        
                        if (ShowFullTime)
                        {
                            row.AddRange(new object[] {
                                session.ConnectTime      != default ? session.ConnectTime      : "",
                                session.HandshakeTime    != default ? session.HandshakeTime    : "",
                                session.LoginStartTime   != default ? session.LoginStartTime   : "",
                                session.LoginSuccessTime != default ? session.LoginSuccessTime : "",
                                session.DisconnectTime   != default ? session.DisconnectTime   : ""});
                        }
                        else if (ShowShortTime)
                        {
                            row.AddRange(new object[] {
                                session.ConnectTime      != default ? session.ConnectTime      : "",
                                session.DisconnectTime   != default ? session.DisconnectTime   : ""});
                        }
                    }

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