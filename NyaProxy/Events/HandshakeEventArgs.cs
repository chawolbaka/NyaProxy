using MinecraftProtocol.Packets.Client;
using NyaProxy.API;
using NyaProxy.API.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy
{
    public class HandshakeEventArgs : TransportEventArgs, IHandshakeEventArgs
    {
        public Socket Source { get; set; }
        
        public HandshakePacket Packet { get; set; }
        
        public HandshakeEventArgs()
        {

        }

        public HandshakeEventArgs(Socket source, HandshakePacket packet)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
        }
    }
}
