using MinecraftProtocol.Utils;
using NyaProxy.API;
using NyaProxy.Extension;
using StringTables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace NyaProxy
{
    public static class Crash
    {

        public static void Report(Exception exception, bool writeConsole = true, bool writeFile = true ,bool exit = true)
        {
            if (exception is null)
                return;

            StringBuilder report = new StringBuilder();
            report.AppendLine($"---- {nameof(NyaProxy)} Crash Report ----").AppendLine();
            report.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ffff K}");
            report.AppendLine($"Source: {exception.Source}").AppendLine();
            
            report.AppendLine(exception.ToString()).AppendLine().AppendLine();
            report.AppendLine("A detailed walkthrough of the error");
            report.AppendLine("---------------------------------------------------------------------------------------").AppendLine().AppendLine();
            report.AppendLine($"  Thread Name: {Thread.CurrentThread.Name}");
            report.AppendLine($"  Managed Thread Id: {Thread.CurrentThread.ManagedThreadId}");
            report.AppendLine($"  Background Thread: {Thread.CurrentThread.IsBackground}");
            report.AppendLine($"  Thread Pool Thread: {Thread.CurrentThread.IsThreadPoolThread}");
            report.AppendLine();

            StringTable bridgeTable = DebugHelper.CreateBridgeTable();
            if (bridgeTable is not null && bridgeTable.Rows.Count > 0)
            {
                report.AppendLine($"  Current Connections({bridgeTable.Rows}) :");
                report.AppendLine(bridgeTable.ToString());
            }

            if (NyaProxy.Plugin is not null && NyaProxy.Plugin.Count > 0)
            {
                report.AppendLine($"  Plugins({NyaProxy.Plugin.Count}) :");
                report.AppendLine(DebugHelper.CreatePluginTable().ToString());
            }

            report.AppendLine("-- System Details --");
            report.AppendLine("Details:");
            report.AppendLine($"  {nameof(NyaProxy)} Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            report.AppendLine($"  Operating System: {Environment.OSVersion} ({(IntPtr.Size == 4 ? "32" : "64")})");
            report.AppendLine($"  DONET Version: {Environment.Version}");
            if (Environment.GetCommandLineArgs().Length > 1)
                report.AppendLine($"  Command Line Args: {string.Join(' ', Environment.GetCommandLineArgs().AsSpan().Slice(1).ToArray())}");

            if (writeConsole)
            {
                ConsoleColor color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(report);
                Console.ForegroundColor = color;
            }
                

            if (writeFile)
            {
                string path = "Crash-Reports";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, $"crash-{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.txt"), report.ToString());

            }

            if (exit)
            {
#if DEBUG
                Console.ReadKey();
#endif
                Environment.Exit(-233);
            }
        }
    }
}
