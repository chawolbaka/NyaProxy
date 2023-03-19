using System;
using System.IO;
using NyaProxy.API.Config;
using NyaProxy.API.Command;
using NyaProxy.API.Channle;

namespace NyaProxy.API
{
    public interface IPluginHelper
    {
        DirectoryInfo WorkDirectory { get; }
        IEvents Events { get; }
        INetworkHelper Network { get; }
        IHostContainer Hosts { get; }
        IChannleContainer Channles { get; }
        IConfigContainer Config { get; }
        ICommandContainer Command { get; }
    }
}
