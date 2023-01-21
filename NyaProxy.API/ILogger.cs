using System;

namespace NyaProxy.API
{
    public interface ILogger
    {
        ILogger Info(string message);
        ILogger Warn(string message);
        ILogger Trace(string message);
        ILogger Debug(string message);
        ILogger Error(string message);
        ILogger Exception(Exception exception);
        ILogger Unpreformat(string message);
        ILogger UnpreformatColorfully(string message);
        
    }
}
