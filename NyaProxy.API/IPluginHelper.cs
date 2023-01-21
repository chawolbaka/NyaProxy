using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace NyaProxy.API
{
    public interface IPluginHelper
    {
        DirectoryInfo WorkDirectory { get; }
        IEvents Events { get; }
        INetworkHelper Network { get; }
        IConfigHelper Config { get; }
        IReadOnlyDictionary<string, IHost> Hosts { get; }
        IReadOnlyDictionary<Guid, IBridge> Bridges { get; }

        ///// <summary>
        ///// 加载配置文件
        ///// </summary>
        ///// <param name="fileName">配置文件名</param>
        //ITomlConfig LoadConfig(string fileName);

    }
}
