using MinecraftProtocol.Utils;
using System.Net.Sockets;

namespace NyaProxy.API
{
    public interface IDisconnectEventArgs : ICancelEvent
    {
        string Host { get; }
    }
}