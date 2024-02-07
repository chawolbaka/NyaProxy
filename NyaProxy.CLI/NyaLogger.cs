using ConsolePlus;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace NyaProxy.CLI

{
    public class NyaLogger : ILogger
    {
        private readonly BlockingCollection<(DateTime Time, LogLevel Level, string Message)> _logQueues = new();
        private bool _started = false;

        public void Listen()
        {
            if (_started)
                throw new InvalidOperationException("Started.");
            _started = true;

            ColorfullyConsole.Init();
            Thread thread = new Thread(StartListen);
            thread.Start();
        }

        private void StartListen()
        {
            FileStream fileStream = null!;
            DateTime time = DateTime.Now;
            if (NyaProxy.Config.LogFile.Enable)
            {
                LogFile config = NyaProxy.Config.LogFile;
                if (!Directory.Exists(config.Directory))
                    Directory.CreateDirectory(config.Directory);
                fileStream = GetLogFileStream(config);
            }
            while (!NyaProxy.GlobalQueueToken.IsCancellationRequested)
            {
                var log = _logQueues.Take();
                DateTime now = DateTime.Now;
                if (NyaProxy.Config.LogFile.Enable)
                {
                    try
                    {
                        if (now.Day != time.Day)
                        {
                            fileStream.Flush();
                            fileStream.Close();
                            fileStream = GetLogFileStream(NyaProxy.Config.LogFile);
                            time = DateTime.Now;
                        }
                        //记得去掉颜色代码
                        fileStream.Write(Encoding.UTF8.GetBytes($"[{log.Time:yyyy-MM-dd HH:mm:ss}] [{GetLogLevelString(log.Level)}] : {log.Message}"));
                        fileStream.Flush();
                    }
                    catch (IOException e)
                    {
                        this.LogError(e);
                        fileStream = GetLogFileStream(NyaProxy.Config.LogFile);
                    }
                }
                ColorfullyConsole.WriteLine($"{GetLogLevelColorCode(log.Level)}[{log.Time:HH:mm:ss}] [{GetLogLevelString(log.Level)}] : {log.Message}");
            }
        }

        private FileStream GetLogFileStream(LogFile config)
        {
            string file = Path.Combine(config.Directory, DateTime.Now.ToString(NyaProxy.Config.LogFile.Format) + ".log");

            if (File.Exists(file))
            {
                int count = -1;
                while (File.Exists(file + $".{++count}")) { }
                File.Move(file, file + $".{count}");
            }
            FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate);
            return fileStream;
        }

        private string GetLogLevelColorCode(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Information: return ColorfullyConsole.DefaultColorCodeSymbol + "f";
                case LogLevel.Warning:     return ColorfullyConsole.DefaultColorCodeSymbol + "e";
                case LogLevel.Error:       return ColorfullyConsole.DefaultColorCodeSymbol + "c";
                case LogLevel.Debug:       return ColorfullyConsole.DefaultColorCodeSymbol + "8";
                default: return "";
            }
        }

        private string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Information: return "Info";
                case LogLevel.Warning:     return "Warn";
                default: return logLevel.ToString();
            }
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logQueues.Add((DateTime.Now, logLevel, formatter(state, exception)));
        }
    }
}
