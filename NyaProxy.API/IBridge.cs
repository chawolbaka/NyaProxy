using MinecraftProtocol.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public interface IBridge
    {
        /// <summary>
        /// 会话Id
        /// </summary>
        long SessionId { get; }

        /// <summary>
        /// 客户端的Socket
        /// </summary>
        Socket Source { get; }

        /// <summary>
        /// 服务端的Socket
        /// </summary>
        Socket Destination { get; }

        /// <summary>
        /// 断开连接
        /// </summary>
        void Break();
    }
}
