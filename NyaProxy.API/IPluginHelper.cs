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
        /// <summary>
        /// 插件的工作目录
        /// </summary>
        DirectoryInfo WorkDirectory { get; }

        /// <summary>
        /// 事件相关
        /// </summary>
        IEvents Events { get; }

        /// <summary>
        /// 网络相关
        /// </summary>
        INetworkHelper Network { get; }

        /// <summary>
        /// 现有的连接
        /// </summary>
        IReadOnlyDictionary<long, IBridge> Bridges { get; }

        /// <summary>
        /// 现有的Host
        /// </summary>
        IHostContainer Hosts { get; }

        /// <summary>
        /// 插件频道相关（这个插件频道是一种数据包，不是指NyaProxy的插件）
        /// </summary>
        IChannleContainer Channles { get; }

        /// <summary>
        /// 配置文件相关
        /// </summary>
        IConfigContainer Config { get; }

        /// <summary>
        /// 指令相关
        /// </summary>
        ICommandContainer Command { get; }
    }
}
