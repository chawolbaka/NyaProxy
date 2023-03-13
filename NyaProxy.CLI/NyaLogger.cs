using NLog;
using System;

namespace NyaProxy.CLI
{
    public class NyaLogger : API.ILogger
    {
        public Logger Logger { get; set; }

        public NyaLogger(Logger logger)
        {
            Logger = logger;
        }

        public API.ILogger Debug(string message)
        {
            Logger.Debug(message);
            return this;
        }

        public API.ILogger Error(string message)
        {
            Logger.Error(message);
            return this;
        }

        public API.ILogger Exception(Exception exception)
        {
            Logger.Error(exception.Message);
            Console.WriteLine(exception);
            return this;
        }

        public API.ILogger Info(string message)
        {
            Logger.Info(message);
            return this;
        }

        public API.ILogger Trace(string message)
        {
            Logger.Trace(message);
            return this;
        }

        public API.ILogger Unpreformat(string message)
        {
            Console.WriteLine(message);
            return this;
        }

        public API.ILogger UnpreformatColorfully(string message)
        {
            ConsolePlus.ColorfullyConsole.WriteLine(message);
            return this;
        }

        public API.ILogger Warn(string message)
        {
            Logger.Warn(message);
            return this;
        }
    }
}
