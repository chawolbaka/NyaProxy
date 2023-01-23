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
            var bridges = NyaProxy.Bridges?.ToArray();
            if (bridges is not null && bridges.Length > 0)
            {

                int count = 0;
                StringTable table = new StringTable("Session Id", "Host", "Player", "Source", "Destination");
                foreach (var host in bridges)
                {
                    foreach (var bridge in host.Value.Values)
                    {
                        IServer server = (bridge as BlockingBridge)?.Server;
                        IPlayer player = (bridge as BlockingBridge)?.Player;
                        string playerName = player is not null ? player.Name : "";
                        table.AddRow(bridge.SessionId.ToString().Replace("-", ""), host.Key, playerName,
                             $"{bridge.Source._remoteEndPoint()} ({(NetworkUtils.CheckConnect(bridge.Source) ? "Online" : "Offline")})",
                             $"{bridge.Destination._remoteEndPoint()} ({(NetworkUtils.CheckConnect(bridge.Destination) ? "Online" : "Offline")})");
                        count++;
                    }
                }
                report.AppendLine().AppendLine($"  Current Connection Count: {count}");
                report.AppendLine($"  Current Connections");
                report.AppendLine(table.ToString());
            }
            if (NyaProxy.Plugin is not null && NyaProxy.Plugin.Count > 0)
            {
                report.AppendLine($"  Plugin Count: {NyaProxy.Plugin.Count}");
                report.AppendLine($"  Plugins");
                StringTable table = new StringTable("Id", "Name", "Version", "Work Directory");
                foreach (var pair in NyaProxy.Plugin.Plugins)
                {
                    var plugin = pair.Value.Plugin.Manifest;
                    table.AddRow(plugin.UniqueId, plugin.Name, plugin.Version, pair.Value.Plugin.Helper.WorkDirectory.Name);
                }
                report.AppendLine(table.ToString());
            }
            report.AppendLine();
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
