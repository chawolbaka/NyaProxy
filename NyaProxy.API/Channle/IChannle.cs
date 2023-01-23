using System;
using System.Net.Sockets;

namespace NyaProxy.API.Channle
{
    public interface IChannle
    {
        /// <summary>
        /// 频道名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 注册频道消息
        /// </summary>
        Guid RegisterMessage(IChannleMessage handler);

        /// <summary>
        /// 注册forge频道消息
        /// </summary>
        /// <param name="discriminator">这边有跨语言的问题，java那边的sbyte是C#这边的byte</param>
        Guid RegisterForgeMessage(IForgeChannleMessage handler, sbyte discriminator);

        /// <summary>
        /// 取消注册频道消息
        /// </summary>
        /// <param name="id">注册时返回的id</param>
        /// <returns>是否成功被取消注册</returns>
        bool UnregisterMessage(Guid id);

        void SendMessage(IChannleMessage handler, Socket dest);
        void SendForgeMessage(IForgeChannleMessage handler, sbyte discriminator, Socket dest);
    }
}
