using MinecraftProtocol.Utils;
using System.Net.Sockets;

namespace NyaProxy.API
{
    public interface IConnectEventArgs : ICancelEvent, IBlockEventArgs
    {
        long SessionId { get; }
        Socket AcceptSocket { get; set; }
        SocketError SocketError { get; }
    }
}