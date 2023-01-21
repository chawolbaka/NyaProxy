using System;
using MinecraftProtocol.Utils;

namespace NyaProxy.API
{
    public interface ITransportEvent
    {
        event EventHandler<IConnectEventArgs> Connecting;
        event EventHandler<IHandshakeEventArgs> Handshaking;
        event EventHandler<ILoginStartEventArgs> LoginStart;
        event EventHandler<ILoginSuccessEventArgs> LoginSuccess;
        event EventHandler<IPacketSendEventArgs> PacketSendToClient;
        event EventHandler<IPacketSendEventArgs> PacketSendToServer;
        event EventHandler<IChatSendEventArgs> ChatMessageSendToClient;
        event EventHandler<IChatSendEventArgs> ChatMessageSendToServer;
        event AsyncCommonEventHandler<object, IAsyncChatEventArgs> ChatMessageSened;
        event EventHandler<IDisconnectEventArgs> Disconnected;
    }
}
