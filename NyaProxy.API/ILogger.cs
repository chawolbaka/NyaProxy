using System;
using System.IO;

namespace NyaProxy.API
{

    public interface INyaLogger
    {
        LogFile LogFile { get; set; }

        INyaLogger Info(string message);
        INyaLogger Warn(string message);
        INyaLogger Trace(string message);
        INyaLogger Debug(string message);
        INyaLogger Error(string message);
        INyaLogger Exception(Exception exception);
        INyaLogger Unpreformat(string message);
        
    }
}
