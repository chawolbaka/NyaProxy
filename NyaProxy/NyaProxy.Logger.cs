using NyaProxy.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy
{
    public static partial class NyaProxy
    {
        public static INyaLogger Logger => _logger;

        private static List<Type> _loggerTypes = new List<Type>();

        public static void AddLogger<T>() where T : INyaLogger, new()
        {
            _loggerTypes.Add(typeof(T));
            _logger = CreateLogger();
        }

        internal static INyaLogger CreateLogger()
        {
            if (_loggerTypes.Count == 1)
                return (INyaLogger)Activator.CreateInstance(_loggerTypes[0]);
            else
                return new MultipleLogger(_loggerTypes.ToArray());
        }

        private static INyaLogger _logger = new EmptyLogger();

        private class EmptyLogger : INyaLogger
        {
            public string Prefix { get; set; }

            public INyaLogger Debug(string message)
            {
                return this;
            }

            public INyaLogger Error(string message)
            {
                return this;
            }

            public INyaLogger Exception(Exception exception)
            {
                return this;
            }

            public INyaLogger Info(string message)
            {
                return this;
            }

            public INyaLogger Trace(string message)
            {
                return this;
            }

            public INyaLogger Unpreformat(string message)
            {
                return this;
            }

            public INyaLogger Warn(string message)
            {
                return this;
            }
        }
        private class MultipleLogger : INyaLogger
        {
            string INyaLogger.Prefix
            {
                get => _prefix;
                set
                {
                    _prefix = value;
                    foreach (var logger in LoggerList)
                    {
                        logger.Prefix = _prefix;
                    }
                }
            }

            private string _prefix;
            private List<INyaLogger> LoggerList = new List<INyaLogger>();

            public MultipleLogger(params Type[] types)
            {
                foreach (var type in types)
                {
                    LoggerList.Add((INyaLogger)Activator.CreateInstance(type));
                }
            }

            INyaLogger INyaLogger.Debug(string message)
            {
                foreach (var logger in LoggerList)
                {
                    logger.Debug(message);
                }
                return this;
            }

            INyaLogger INyaLogger.Error(string message)
            {
                foreach (var logger in LoggerList)
                {
                    logger.Error(message);
                }
                return this;
            }

            INyaLogger INyaLogger.Exception(Exception exception)
            {
                foreach (var logger in LoggerList)
                {
                    logger.Exception(exception);
                }
                return this;
            }

            INyaLogger INyaLogger.Info(string message)
            {
                foreach (var logger in LoggerList)
                {
                    logger.Info(message);
                }
                return this;
            }

            INyaLogger INyaLogger.Trace(string message)
            {
                foreach (var logger in LoggerList)
                {
                    logger.Trace(message);
                }
                return this;
            }

            INyaLogger INyaLogger.Unpreformat(string message)
            {
                foreach (var logger in LoggerList)
                {
                    logger.Unpreformat(message);
                }
                return this;
            }

            INyaLogger INyaLogger.Warn(string message)
            {
                foreach (var logger in LoggerList)
                {
                    logger.Warn(message);
                }
                return this;
            }
        }
    }
}
