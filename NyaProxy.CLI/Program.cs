using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using ConsolePlus;
using ConsolePlus.InteractiveMode;
using Microsoft.Extensions.Logging;
using NyaProxy.API.Command;
using NyaProxy.CLI.Commands;
using NyaProxy.Debug;
using StringTable;

namespace NyaProxy.CLI
{
    partial class Program
    {
        
        private static async Task Main(string[] args)
        {
            NyaLogger logger = new NyaLogger();
            logger.Listen();
            await NyaProxy.SetupAsync(logger);

            foreach (var server in NyaProxy.Hosts)
            {
                NyaProxy.Logger.LogInformation($"{server.Value.Name} -> [{string.Join(", ", server.Value.ServerEndPoints.Select(x => x.ToString()))}]");
            }

            NyaProxy.BindSockets();
            NyaProxy.CommandManager.Register(new ConfigCommand());
            NyaProxy.CommandManager.Register(new PluginCommand());
            NyaProxy.CommandManager.Register(new SimpleCommand("stop", async (args, helper) => await ExitAsync()));
            NyaProxy.CommandManager.Register(new SimpleCommand("hosts", async (args, helper) => helper.Logger.LogInformation(string.Join(',', NyaProxy.Hosts.Values.Select(x => x.Name).ToArray()))));
            NyaProxy.CommandManager.Register(new SimpleCommand("plugins", async (args, helper) =>
            {
                if (NyaProxy.Plugins.Count > 0)

                    NyaProxy.Logger.LogMultiLineInformation($"{NyaProxy.Plugins.Count} plugins loaded.", DebugHelper.CreatePluginTable().Export());
                else
                    NyaProxy.Logger.LogInformation("当前没有任何已加载的插件");
            }));

            NyaProxy.CommandManager.Register(new SimpleCommand("bridges", async (args, helper) => {
                StringTableBuilder table = DebugHelper.CreateBridgeTable();
                if (table.NumberOfRows > 0)
                    NyaProxy.Logger.LogMultiLineInformation(table.Export().ToString());
                else
                    NyaProxy.Logger.LogInformation("当前没有建立任何连接。");
            }));

            Console.CancelKeyPress += (sender, e) =>
            {
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    ExitAsync().Wait();
            };

            while (!NyaProxy.GlobalQueueToken.IsCancellationRequested)
            {
                string input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    string[] CommandArgs = TokenToCommandArguments(Token.Parse(input)).ToArray().Where(x => x != null).ToArray();
                    try
                    {
                        if (CommandArgs.Length > 0)
                            await NyaProxy.CommandManager.RunAsync(CommandArgs[0], CommandArgs.AsSpan(1).ToArray(), new CommandHelper());
                    }
                    catch (Exception e)
                    {
                        NyaProxy.Logger.LogError(e);
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


        public static async Task ExitAsync()
        {
            NyaProxy.Logger.LogInformation("stoping...");
            await NyaProxy.StopAsync();
            Environment.Exit(0);
        }
    }
}
