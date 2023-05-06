using System;
using System.Net.Sockets;
using NyaProxy.API.Enum;
using MinecraftProtocol.Utils;
using MinecraftProtocol.Packets;

namespace NyaProxy.API
{

    /// <summary>
    /// 事件结束后内部的成员都将返回对象池内，请不要滞留该实例本身和内部的任何成员。
    /// </summary>
    public interface IPacketSendEventArgs : ICancelEvent, IBlockEventArgs
    {
        long SessionId { get; }

        string Host { get; }

        /// <summary>
        /// 数据包的来源
        /// </summary>
        Socket Source { get; }

        /// <summary>
        /// 数据包将被发送到该套接字（可被修改）
        /// </summary>
        Socket Destination { get; set; }

        /// <summary>
        /// 数据包将被发送到的位置
        /// </summary>
        Direction Direction { get; }

        /// <summary>
        /// 当前的阶段
        /// </summary>
        Stage Stage { get; }

        /// <summary>
        /// 即将被发送的包，可进行修改或直接替换
        /// </summary>
        LazyCompatiblePacket Packet { get; set; }

        /// <summary>
        /// 数据包被接收到的时间
        /// </summary>
        DateTime ReceivedTime { get; }

        /// <summary>
        /// 当前的协议版本
        /// </summary>
        int ProtocolVersion { get; }

        /// <summary>
        /// 当前的压缩阈值
        /// </summary>
        int CompressionThreshold { get; }

        /// <summary>
        /// 玩家（可能为null）
        /// </summary>
        IPlayer Player { get; }

    }
}