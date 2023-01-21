using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Utils;

namespace NyaProxy.API
{
    public interface IHandshakeEventArgs : ICancelEvent, IBlockEventArgs
    {
        HandshakePacket Packet { get; set; }
    }
}