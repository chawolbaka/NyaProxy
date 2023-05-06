using System;
using System.Net.Sockets;
using NyaProxy.API;
using MinecraftProtocol.Packets.Client;

namespace NyaProxy
{
    public class HandshakeEventArgs : TransportEventArgs, IHandshakeEventArgs
    {
        public long SessionId { get; }

        public Socket Source { get; set; }

        public IHost Host { get; set; }

        public HandshakePacket Packet { get; set; }
        
        public HandshakeEventArgs()
        {

        }

        public HandshakeEventArgs(long sessionId, Socket source, HandshakePacket packet,IHost host)
        {
            SessionId = sessionId;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }
    }
}
