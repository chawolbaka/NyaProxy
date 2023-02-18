using System;
using NyaProxy.API;

namespace NyaProxy.Plugin
{
    internal partial class PluginHelper
    {
        private class PluginEvents : IEvents
        {
            public ITransportEvent Transport => _transport;
            private static ITransportEvent _transport = new TransportEvent();

            private class TransportEvent : ITransportEvent
            {
                public event EventHandler<IConnectEventArgs>      Connecting              { add => NyaProxy.Connecting.Add(value);              remove => NyaProxy.Connecting.Remove(value); }
                public event EventHandler<IHandshakeEventArgs>    Handshaking             { add => NyaProxy.Handshaking.Add(value);             remove => NyaProxy.Handshaking.Remove(value); }
                public event EventHandler<ILoginStartEventArgs>   LoginStart              { add => NyaProxy.LoginStart.Add(value);              remove => NyaProxy.LoginStart.Remove(value); }
                public event EventHandler<ILoginSuccessEventArgs> LoginSuccess            { add => NyaProxy.LoginSuccess.Add(value);            remove => NyaProxy.LoginSuccess.Remove(value); }
                public event EventHandler<IPacketSendEventArgs>   PacketSendToClient      { add => NyaProxy.PacketSendToClient.Add(value);      remove => NyaProxy.PacketSendToClient.Remove(value); }
                public event EventHandler<IPacketSendEventArgs>   PacketSendToServer      { add => NyaProxy.PacketSendToServer.Add(value);      remove => NyaProxy.PacketSendToServer.Remove(value); }
                public event EventHandler<IChatSendEventArgs>     ChatMessageSendToClient { add => NyaProxy.ChatMessageSendToClient.Add(value); remove => NyaProxy.ChatMessageSendToClient.Remove(value); }
                public event EventHandler<IChatSendEventArgs>     ChatMessageSendToServer { add => NyaProxy.ChatMessageSendToServer.Add(value); remove => NyaProxy.ChatMessageSendToServer.Remove(value); }
                public event EventHandler<IDisconnectEventArgs>   Disconnected            { add => NyaProxy.Disconnected.Add(value);            remove => NyaProxy.Disconnected.Remove(value); }
            }
        }

    }
}
