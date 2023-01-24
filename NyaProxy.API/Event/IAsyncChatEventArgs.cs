using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Utils;
using NyaProxy.API.Enum;
using System;
using System.Net.Sockets;

namespace NyaProxy.API
{
    public interface IAsyncChatEventArgs : ICancelEvent
    {

        /// <summary>
        /// 收到聊天信息的时间
        /// </summary>
        DateTime ReceivedTime { get; }
        
        /// <summary>
        /// 聊天信息
        /// </summary>
        ChatMessage Message { get; }
    }
}