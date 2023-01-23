using System;
using System.IO;
using System.Collections.Generic;
using NyaProxy.API.Channle;

namespace NyaProxy.API
{
    public interface IPluginHelper
    {
        DirectoryInfo WorkDirectory { get; }
        IEvents Events { get; }
        INetworkHelper Network { get; }
        IConfigHelper Config { get; }
        IReadOnlyDictionary<string, IHost> Hosts { get; }
        IChannleContainer Channles { get; }
    }
}
