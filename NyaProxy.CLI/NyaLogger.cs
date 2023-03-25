using NyaProxy.API;
using System;
using System.Collections.Concurrent;

namespace NyaProxy.CLI
{
    public class NyaLogger : ILogger
    {
        public readonly BlockingCollection<string> LogQueues = new BlockingCollection<string>();
        public string BaseMessage
        {
            get
            {
                string message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]";
                Thread thread = Thread.CurrentThread;
                if (string.IsNullOrWhiteSpace(thread.Name))
                    return message + $" [Craft Thread#{thread.ManagedThreadId}] ";
                else
                    return message + $" [{thread.Name}] ";
            }
        }

        public NyaLogger()
        {

        }

        public ILogger Debug(string message)
        {
            LogQueues.Add($"§8{BaseMessage}[{nameof(Debug)}]: {message}");
            return this;
        }

        public ILogger Error(string message)
        {
            LogQueues.Add($"§c{BaseMessage}[{nameof(Error)}]: {message}");
            return this;
        }

        public ILogger Exception(Exception exception)
        {
            LogQueues.Add($"§c{BaseMessage}[{nameof(Error)}]: {exception.Message}\n{exception}");
            return this;
        }

        public ILogger Info(string message)
        {
            LogQueues.Add($"§f{BaseMessage}[{nameof(Info)}]: {message}");
            return this;
        }

        public ILogger Trace(string message)
        {
            LogQueues.Add($"§7{BaseMessage}[{nameof(Debug)}]: {message}");
            return this;
        }

        public ILogger Warn(string message)
        {
            LogQueues.Add($"§e{BaseMessage}[{nameof(Debug)}]: {message}");
            return this;
        }

        public ILogger Unpreformat(string message)
        {
            LogQueues.Add(message);
            return this;
        }
    }
}
