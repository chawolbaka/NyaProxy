using System;
using System.IO;
using System.Text;
using ConsolePlus;
using NyaProxy.CLI.Commands;

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
            NyaProxy.CommandManager.Register(new ConfigCommand());

            
            FileStream fileStream = null!;
            DateTime time = DateTime.Now;
            if (logger.LogFile.Enable)
            {
                if (!Directory.Exists(logger.LogFile.Directory))
                    Directory.CreateDirectory(logger.LogFile.Directory);
                fileStream = GetLogFileStream(logger);
            }
            while (!NyaProxy.GlobalQueueToken.IsCancellationRequested)
            {

                var log = logger.LogQueues.Take();
                DateTime now = DateTime.Now;
                Thread thread = Thread.CurrentThread;
                string threadName = string.IsNullOrWhiteSpace(thread.Name) ? "[Craft Thread#{thread.ManagedThreadId}]" : thread.Name;

                if (logger.LogFile.Enable)
                {
                    if (now.Day != time.Day)
                    {
                        fileStream.Flush();
                        fileStream.Close();
                        fileStream = GetLogFileStream(logger);
                        time = DateTime.Now;
                    }
                    if (log.Type == LogType.Unpreformat)
                        fileStream.Write(Encoding.UTF8.GetBytes(log.Message + Environment.NewLine));
                    else
                        fileStream.Write(Encoding.UTF8.GetBytes($"[{now:yyyy-MM-dd HH:mm:ss}] [{threadName}] [{log.Type}] : {log.Message}{Environment.NewLine}"));
                    fileStream.Flush();
                }
                if (log.Type == LogType.Unpreformat)
                    ColorfullyConsole.WriteLine(log.Message);
                else
                    ColorfullyConsole.WriteLine($"§{(int)log.Type:x1}[{now:HH:mm:ss}] [{threadName}] [{log.Type}] : {log.Message}");
            }
        }

        private static FileStream GetLogFileStream(NyaLogger logger)
        {
            string file = Path.Combine(logger.LogFile.Directory, DateTime.Now.ToString(logger.LogFile.Format) + ".log");
            
            if(File.Exists(file))
            {
                int count = -1;
                while (File.Exists(file + $".{++count}")) { }
                File.Move(file, file + $".{count}");
            }
            FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate);
            return fileStream;
        }
    }
}
