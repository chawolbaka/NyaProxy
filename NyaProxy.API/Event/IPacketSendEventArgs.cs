using MinecraftProtocol.Utils;
using MinecraftProtocol.Packets;
using NyaProxy.API.Enum;
using System;
using System.Net.Sockets;

namespace NyaProxy.API
{

    /// <summary>
    /// 事件结束后内部的成员都将返回对象池内，请不要滞留该实例本身和内部的任何成员。
    /// </summary>
    public interface IPacketSendEventArgs : ICancelEvent, IBlockEventArgs
    {

        /// <summary>
        /// 数据包将被发送到该套接字
        /// </summary>
        Socket Destination { get; set; }

        /// <summary>
        /// 数据包将被发送到的位置
        /// </summary>
        Direction Direction { get; }

        /// <summary>
        /// 即将被发送的包，可进行修改或直接替换
        /// </summary>
        CompatiblePacket Packet { get; set; }

        /// <summary>
        /// 数据包被接收到的时间
        /// </summary>
        DateTime ReceivedTime { get; }

        int ProtocolVersion { get; }

        IServer Server { get; }

        IPlayer Player { get; }
        

        ///// <summary>
        ///// Packet中的ProtocolVersion
        ///// </summary>
        //int ProtocolVersion { get; }

        ///// <summary>
        ///// Packet中的CompressionThreshold
        ///// </summary>
        //int CompressionThreshold { get; }
    }
}