using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Utils;

namespace NyaProxy.API
{
    public interface ILoginStartEventArgs : ICancelEvent, IBlockEventArgs
    {
        LoginStartPacket Packet { get; set; }
    }
}