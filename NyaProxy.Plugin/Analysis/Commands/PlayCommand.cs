using NyaProxy.API;
using NyaProxy.API.Command;
using StringTable;

namespace Analysis.Commands
{
    public class PlayCommand : Command
    {
        public override string Name => "play";

        public override int MinimumArgs => 0;
        
        public virtual bool ShowUUID { get; set; }

        public virtual bool ShowShortTime { get; set; }

        public virtual bool ShowFullTime { get; set; }

        public virtual bool Reverse { get; set; }

        private const int SINGLE_PAGE_LENGTH = 10;

        public PlayCommand()
        {
            AddOption(new Option("-r", 0, (command, e) => Reverse = true, "--reverse"));
            AddOption(new Option("-t", 0, (command, e) => ShowShortTime = true, "--show-time"));
            AddOption(new Option("--show-time-full", 0, (command, e) => ShowFullTime = true));
            AddOption(new Option("--show-uuid", 0, (command, e) => ShowUUID = true));
        }

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            Reverse = false; ShowShortTime = false; ShowFullTime = false; ShowUUID = false;
            int page = 0;

            if (await base.ExecuteAsync(args.Length > 0 && int.TryParse(args.Span[0], out page) ? args.Slice(1) : args, helper))
            {
                if (AnalysisData.Sessions.Count > 0)
                {
                    StringTableBuilder table = new StringTableBuilder();
                    table.AddColumn("Id", "Host", "Player", "Client", "Server", "Transferred");
                    if (ShowFullTime)
                        table.AddColumn("Connect", "Handshake", "LoginStart", "LoginSuccess", "Disconnect");
                    else if (ShowShortTime)
                        table.AddColumn("Connect", "Disconnect");


                    var Sessions = AnalysisData.Sessions.Values.ToArray();
                    if (Reverse)
                    {
                        for (int i = Sessions.Length - 1; i >= 0; i--)
                        {
                            table.AddRow(GetRow(Sessions[i]));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Sessions.Length; i++)
                        {
                            table.AddRow(GetRow(Sessions[i]));
                        }
                    }


                    try
                    {
                        helper.Logger.Unpreformat(table.Export(page, SINGLE_PAGE_LENGTH));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        helper.Logger.Unpreformat($"页面{page}不存在");
                    }
                }
                else
                {
                    helper.Logger.Unpreformat("当前无任何统计数据");
                }
            }
            return true;
        }

        public List<object> GetRow(SessionAnalysisData session)
        {
            List<object> row = new List<object> {
                            session.SessionId,
                            session.Host   != null ? session.Host.Name : "",
                            session.Player != null ? $"{session.Player.Name}{(ShowUUID? $"({session.Player.Id})":"")}" : "",
                            session.Source != null ? session.Source.Address.ToString() : "",
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
            return row;
        }
    }
}