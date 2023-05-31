using NyaProxy.API.Command;
using StringTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analysis.Commands
{
    public class PacketCommand : Command
    {
        public override string Name => "packet";

        public override string Description => "统计客户端/服务端发送了多少数据包";

        public override int MinimumArgs => 1;

        public PacketCommand()
        {
            RegisterChild(new ClientCommand());
            RegisterChild(new ServerCommand());
        }


        private class ClientCommand : Command
        {
            public override string Name => "client";

            public override int MinimumArgs => 1;

            public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
            {
                if (args.IsEmpty)
                    throw new CommandLeastRequiredException(this, MinimumArgs);

                if (long.TryParse(args.Span[0], out long id) && AnalysisData.Sessions.TryGetValue(id, out var data))
                {
                    if (data.PacketAnalysis.Client.Count > 0)
                        helper.Logger.Unpreformat(BuildTable(data.PacketAnalysis.Client).Export());
                    else
                        helper.Logger.Unpreformat("无可用数据");
                }
                else
                {
                    helper.Logger.Unpreformat("该会话不存在");
                }

                return true;
            }

            public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
            {
                return AnalysisData.Sessions.Keys.Select(x => x.ToString());
            }
        }

        private class ServerCommand : Command
        {
            public override string Name => "server";

            public override int MinimumArgs => 1;

            public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
            {
                if (args.IsEmpty)
                    throw new CommandLeastRequiredException(this, MinimumArgs);

                if (long.TryParse(args.Span[0], out long id) && AnalysisData.Sessions.TryGetValue(id, out var data))
                {
                    if (data.PacketAnalysis.Client.Count > 0)
                        helper.Logger.Unpreformat(BuildTable(data.PacketAnalysis.Server).Export());
                    else
                        helper.Logger.Unpreformat("无可用数据");
                }
                else
                {
                    helper.Logger.Unpreformat("该会话不存在");
                }

                return true;
            }

            public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
            {
                return AnalysisData.Sessions.Keys.Select(x => x.ToString());
            }
        }

        private static StringTableBuilder BuildTable(PacketRecord data)
        {
            StringTableBuilder table = new StringTableBuilder();
            table.AddColumn("Id", "Count", "Transferred");
            foreach (var item in data.Table)
            {
                table.AddRow($"0x{item.Key:X2}", item.Value.Count, Utils.SizeSuffix(item.Value.BytesTransferred));
            }
            table.AddRow("Total: ", data.Count, Utils.SizeSuffix(data.BytesTransferred));
            return table;
        }
    }
}
