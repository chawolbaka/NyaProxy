using NyaProxy.API;
using System;
using System.Collections.Concurrent;

namespace NyaProxy.CLI
{
    public class NyaLogger : ILogger
    {
        public readonly BlockingCollection<string> LogQueues = new BlockingCollection<string>();
        public string BaseMessage => $"§f[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{Thread.CurrentThread.Name}] ";

        public NyaLogger()
        {
            
        }

        public ILogger Debug(string message)
        {
            LogQueues.Add($"{BaseMessage}[{nameof(Debug)}]: {message}");
            return this;
        }

        public ILogger Error(string message)
        {
            LogQueues.Add($"{BaseMessage}[{nameof(Error)}]: {message}");
            return this;
        }

        public ILogger Exception(Exception exception)
        {
            LogQueues.Add($"{BaseMessage}[{nameof(Error)}]: {exception.Message}\n{exception}");
            return this;
        }

        public ILogger Info(string message)
        {
            LogQueues.Add($"{BaseMessage}[{nameof(Info)}]: {message}");
            return this;
        }

        public ILogger Trace(string message)
        {
            LogQueues.Add($"{BaseMessage}[{nameof(Trace)}]: {message}");
            return this;
        }

        public ILogger Warn(string message)
        {
            LogQueues.Add($"{BaseMessage}[{nameof(Warn)}]: {message}");
            return this;
        }

        public ILogger Unpreformat(string message)
        {
            LogQueues.Add( message);
            return this;
        }
    }
}
