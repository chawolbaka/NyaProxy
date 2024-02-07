using Microsoft.Extensions.Logging;
using NyaProxy.API.Command;
using StringTable;

namespace Analysis.Commands
{
    public class ServerListPingCommand : Command
    {
        public override string Name => "slp";

        public override async Task<bool> ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
        {
            if (AnalysisData.Pings.Count > 0)
            {
                StringTableBuilder table = new StringTableBuilder();
                table.AddColumn("Client", "Server", "Transferred", "Count");
                foreach (var group in AnalysisData.Pings.Where(x => x != null).GroupBy(p => new { p.Host, p.Source.Address, p.Destination }))
                {
                    long transferred = 0;
                    int count = 0;
                    foreach (var pa in group)
                    {
                        transferred += pa.BytesTransferred;
                        count++;
                    }

                    var firstPA = group.First();
                    table.AddRow(firstPA?.Source?.Address, $"{firstPA?.Host?.Name} [{firstPA?.Destination}]", Utils.SizeSuffix(transferred), count);
                }

                helper.Logger.LogMultiLineInformation(table.Export());
            }
            else
            {
                helper.Logger.LogInformation("当前无任何连接。");
            }

            return true;
        }
    }
}