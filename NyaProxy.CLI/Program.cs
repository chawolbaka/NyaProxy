using System;
using NLog;
using ConsolePlus;

namespace NyaProxy.CLI
{
    partial class Program
    {
        private static async Task Main(string[] args)
        {
            ColorfullyConsole.Init();
            await NyaProxy.Setup(new NyaLogger(LogManager.GetCurrentClassLogger()));
            
            foreach (var server in NyaProxy.Hosts)
            {
                NyaProxy.Logger.Info($"{server.Value.Name} -> [{string.Join(", ", server.Value.ServerEndPoints.Select(x => x.ToString()))}]");
            }
            NyaProxy.BindSockets();
            Console.ReadKey();
        }
    }
}
