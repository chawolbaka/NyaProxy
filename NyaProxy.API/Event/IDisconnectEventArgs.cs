using MinecraftProtocol.Utils;
using System.Net.Sockets;

namespace NyaProxy.API
{
    public interface IDisconnectEventArgs : ICancelEvent
    {
        long SessionId { get; }
        IHost Host { get; }
    }
}