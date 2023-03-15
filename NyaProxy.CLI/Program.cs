using System;
using ConsolePlus;

namespace NyaProxy.CLI
{
    partial class Program
    {
        private static async Task Main(string[] args)
        {
            ColorfullyConsole.Init();
            NyaLogger logger = new NyaLogger();
            await NyaProxy.Setup(logger);
            
            foreach (var server in NyaProxy.Hosts)
            {
                NyaProxy.Logger.Info($"{server.Value.Name} -> [{string.Join(", ", server.Value.ServerEndPoints.Select(x => x.ToString()))}]");
            }
            NyaProxy.BindSockets();

            while (!NyaProxy.GlobalQueueToken.IsCancellationRequested)
            {
                ColorfullyConsole.WriteLine(logger.LogQueues.Take());
            }
        }
    }
}
