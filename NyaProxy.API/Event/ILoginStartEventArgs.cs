using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Utils;

namespace NyaProxy.API
{
    public interface ILoginStartEventArgs : IPacketSendEventArgs
    {
        string PlayerName { get; set; }
        UUID PlayerUUID { get; set; }
    }
}