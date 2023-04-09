using System;
using System.IO;
using NyaProxy.API.Config;
using NyaProxy.API.Command;
using NyaProxy.API.Channle;
using System.Collections.Generic;

namespace NyaProxy.API
{
    public interface IPluginHelper
    {
        DirectoryInfo WorkDirectory { get; }
        IEvents Events { get; }
        INetworkHelper Network { get; }
        IReadOnlyDictionary<long, IBridge> Bridges { get; }
        IHostContainer Hosts { get; }
        IChannleContainer Channles { get; }
        IConfigContainer Config { get; }
        ICommandContainer Command { get; }
    }
}
