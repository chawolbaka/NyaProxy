using CZGL.SystemInfo;
using StringTable;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace NyaProxy.Debug
{

    public static class Crash
    {

        public static void Report(Exception exception, bool writeConsole = true, bool writeFile = true, bool exit = true)
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
            report.AppendLine("-- System Details --");
            report.AppendLine("Details:");
            report.AppendLine($"  {nameof(NyaProxy)} Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            report.AppendLine($"  Operating System: {Environment.OSVersion} ({(IntPtr.Size == 4 ? "32" : "64")})");
            report.AppendLine($"  DONET Version: {Environment.Version}");
            if (Environment.GetCommandLineArgs().Length > 1)
                report.AppendLine($"  Command Line Args: {string.Join(' ', Environment.GetCommandLineArgs().AsSpan().Slice(1).ToArray())}");
            report.AppendLine($"  Memory: {GC.GetTotalAllocatedBytes()} bytes allocated");

            MemoryValue memory = OperatingSystem.IsWindows() ? WindowsMemory.GetMemory() : LinuxMemory.GetMemory();
            report.AppendLine($"  Physical memory max (MB): {memory.AvailablePhysicalMemory / 1024.0}");
            report.AppendLine($"  Physical memory used (MB): {memory.UsedPhysicalMemory / 1024.0}");
            report.AppendLine($"  Virtual memory max (MB): {memory.AvailableVirtualMemory / 1024.0}");
            report.AppendLine($"  Virtual memory used (MB): {memory.UsedVirtualMemory / 1024.0}");
            NetworkInfo[] networkInfos = NetworkInfo.GetNetworkInfos();
            for (int i = 0; i < networkInfos.Length; i++)
            {
                report.AppendLine($"  Network card #{i} Type: {networkInfos[i].NetworkType}");
                report.AppendLine($"  Network card #{i} Name: {networkInfos[i].Name}");
                report.AppendLine($"  Network card #{i} Status: {networkInfos[i].Status}");
                report.AppendLine($"  Network card #{i} Speed: {networkInfos[i].Speed} bytes");
                report.AppendLine($"  Network card #{i} Gateway Addresses: {networkInfos[i].GatewayAddresses.GetAddressString()}");
                report.AppendLine($"  Network card #{i} Unicast Addresses: {networkInfos[i].UnicastAddresses.GetAddressString()}");                
                report.AppendLine($"  Network card #{i} Dns Addresses: {networkInfos[i].DnsAddresses.GetAddressString()}");

                report.AppendLine();

            }



            if (NyaProxy.Bridges != null)
            {
                StringTableBuilder bridgeTable = DebugHelper.CreateBridgeTable();
                if (bridgeTable is not null && bridgeTable.NumberOfRows > 0)
                {
                    report.AppendLine($"  Current Connections({bridgeTable.NumberOfRows}) :");
                    report.AppendLine(bridgeTable.ToString());
                }
            }

            if (NyaProxy.Plugins is not null && NyaProxy.Plugins.Count > 0)
            {
                report.AppendLine($"  Plugins({NyaProxy.Plugins.Count}) :");
                report.AppendLine(DebugHelper.CreatePluginTable().ToString());
            }



            if (writeConsole)
            {
                ConsoleColor color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exception.ToString());
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
                NyaProxy.GlobalQueueToken.Cancel();
                Thread.Sleep(2000);
#if DEBUG
                Console.ReadKey();
#endif
                Environment.Exit(-233);
            }
        }
        private static string GetAddressString(this IPAddress[] ips)
        {
            string result = string.Join(", ", ips.Select(a => a.ToString().Contains(':') ? $"[{a}]" : a.ToString()));

            if (string.IsNullOrEmpty(result))
                return "Empty";
            else
                return result;
        }
    }
}
