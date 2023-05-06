using System.Net.Sockets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Utils;


namespace NyaProxy.API
{
    public interface IHandshakeEventArgs : ICancelEvent, IBlockEventArgs
    {

        /// <summary>
        /// 会话Id
        /// </summary>
        long SessionId { get; }

        /// <summary>
        /// 数据包来源
        /// </summary>
        Socket Source { get; }

        /// <summary>
        /// 目标Host
        /// </summary>
        IHost Host { get; }

        /// <summary>
        /// 即将被发送的包，可进行修改或直接替换
        /// </summary>
        HandshakePacket Packet { get; set; }

        
    }
}