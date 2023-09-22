using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using ConsolePlus;
using ConsolePlus.InteractiveMode;
using NyaProxy.API.Command;
using NyaProxy.CLI.Commands;
using NyaProxy.Debug;
using StringTable;

namespace NyaProxy.CLI
{
    partial class Program
    {
        public static NyaLogger Logger;
        private static async Task Main(string[] args)
        {
            ColorfullyConsole.Init();
            Logger = new NyaLogger();
            await NyaProxy.Setup(Logger);

            foreach (var server in NyaProxy.Hosts)
            {
                NyaProxy.Logger.Info($"{server.Value.Name} -> [{string.Join(", ", server.Value.ServerEndPoints.Select(x => x.ToString()))}]");
            }

            NyaProxy.BindSockets();
            NyaProxy.CommandManager.Register(new ConfigCommand());
            NyaProxy.CommandManager.Register(new PluginCommand());
            NyaProxy.CommandManager.Register(new SimpleCommand("stop", async (args, helper) => await ExitAsync()));
            NyaProxy.CommandManager.Register(new SimpleCommand("hosts", async (args, helper) =>
                helper.Logger.Unpreformat(string.Join(',', NyaProxy.Hosts.Values.Select(x => x.Name).ToArray()))));
            NyaProxy.CommandManager.Register(new SimpleCommand("plugins", async (args, helper) =>
                helper.Logger.Unpreformat(NyaProxy.Plugins.Count > 0 ? DebugHelper.CreatePluginTable().Export() : "当前没有任何已加载的插件")));

            Console.CancelKeyPress += (sender, e) =>
            {
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    ExitAsync().Wait();
            };

            new Thread(ListenLog) { IsBackground = true }.Start();

            while (true)
            {
                string input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    string[] CommandArgs = TokenToCommandArguments(Token.Parse(input)).ToArray().Where(x => x != null).ToArray();
                    try
                    {
                        if (CommandArgs.Length > 0)
                            await NyaProxy.CommandManager.RunAsync(CommandArgs[0], CommandArgs.AsSpan().Slice(1).ToArray(), new CommandHelper(NyaProxy.Logger));
                    }
                    catch (Exception e)
                    {
                        Logger.Exception(e);
                    }
                }
            }

        }

        public static List<string> TokenToCommandArguments(IList<Token> tokens)
        {
            List<string> arguments = new List<string>(tokens.Count);
            StringBuilder CommandArgs = new StringBuilder();
            for (int i = 0; i < tokens.Count; i++)
            {
                switch (tokens[i].Kind)
                {
                    case SyntaxKind.StringToken: CommandArgs.Append(tokens[i].Value); break;
                    case SyntaxKind.SplitToken: arguments.Add(CommandArgs.ToString()); CommandArgs.Clear(); break;
                    case SyntaxKind.EndToken:
                        if (CommandArgs.Length > 0)
                            arguments.Add(CommandArgs.ToString());
                        break;
                }
            }
            return arguments;
        }

        private static void ListenLog()
        {
            FileStream fileStream = null!;
            DateTime time = DateTime.Now;
            if (Logger.LogFile.Enable)
            {
                if (!Directory.Exists(Logger.LogFile.Directory))
                    Directory.CreateDirectory(Logger.LogFile.Directory);
                fileStream = GetLogFileStream(Logger);
            }
            while (!NyaProxy.GlobalQueueToken.IsCancellationRequested)
            {
                var log = Logger.LogQueues.Take();
                DateTime now = DateTime.Now;
                Thread thread = Thread.CurrentThread;
                string threadName = string.IsNullOrWhiteSpace(thread.Name) ? "[Craft Thread#{thread.ManagedThreadId}]" : thread.Name;

                if (Logger.LogFile.Enable)
                {
                    if (now.Day != time.Day)
                    {
                        fileStream.Flush();
                        fileStream.Close();
                        fileStream = GetLogFileStream(Logger);
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

        public static async Task ExitAsync()
        {
            NyaProxy.Logger.Info("stoping...");
            await NyaProxy.StopAsync();
            Environment.Exit(0);
        }
    }
}
