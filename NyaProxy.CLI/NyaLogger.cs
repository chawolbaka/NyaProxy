using System.Collections.Concurrent;
using NyaProxy.API;

namespace NyaProxy.CLI
{

    public class NyaLogger : INyaLogger
    {
        public static readonly BlockingCollection<(string Message, LogType Type)> LogQueues = new();

        public string Prefix { get; set; }

        public INyaLogger Debug(string message)
        {
            LogQueues.Add((Prefix + message, LogType.Debug));
            return this;
        }

        public INyaLogger Error(string message)
        {
            LogQueues.Add((Prefix + message, LogType.Error));
            return this;
        }

        public INyaLogger Exception(Exception exception)
        {
            LogQueues.Add(($"{Prefix}{exception.Message}\n{exception}", LogType.Error));
            return this;
        }

        public INyaLogger Info(string message)
        {
            LogQueues.Add((Prefix + message, LogType.Info));
            return this;
        }

        public INyaLogger Trace(string message)
        {
            LogQueues.Add((Prefix + message, LogType.Trace));
            return this;
        }

        public INyaLogger Warn(string message)
        {
            LogQueues.Add((Prefix + message, LogType.Warn));
            return this;
        }

        public INyaLogger Unpreformat(string message)
        {
            LogQueues.Add((Prefix + message, LogType.Unpreformat));
            return this;
        }
    }
}
