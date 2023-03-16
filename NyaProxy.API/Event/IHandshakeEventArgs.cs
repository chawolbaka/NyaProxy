using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Utils;
using NyaProxy.API.Enum;
using System.Net.Sockets;

namespace NyaProxy.API
{
    public interface IHandshakeEventArgs : ICancelEvent, IBlockEventArgs
    {
        /// <summary>
        /// 数据包来源
        /// </summary>
        Socket Source { get; }

        IHostConfig HostConfig { get; }

        /// <summary>
        /// 即将被发送的包，可进行修改或直接替换
        /// </summary>
        HandshakePacket Packet { get; set; }

        
    }
}