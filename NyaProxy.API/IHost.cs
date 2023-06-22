using NyaProxy.API.Enum;
using System;
using System.Collections.Generic;
using System.Net;

namespace NyaProxy.API
{
    public interface IHost
    {
        /// <summary>
        /// 主机名
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// 服务端地址
        /// </summary>
        List<EndPoint> ServerEndPoints { get; }

        /// <summary>
        /// 如何选择服务端地址（如果有多个）
        /// </summary>
        ServerSelectMode SelectMode { get; }
        

        /// <summary>
        /// 如何处理连入该Host的连接
        /// </summary>
        ForwardMode ForwardMode { get; }

        ServerFlags Flags { get; }

        /// <summary>
        /// 客户端到代理端之间的压缩阈值，如果-1那么就使用服务端的压缩阈值
        /// </summary>

        int CompressionThreshold { get; }
        

        /// <summary>
        /// 服务端的协议号，如果-1那么就直接转发客户端在握手包中使用的协议号
        /// </summary>
        int ProtocolVersion { get; }
        
        /// <summary>
        /// 该Host下的现有连接
        /// </summary>
        IReadOnlyDictionary<long, IBridge> Bridges { get; }
    }
}
