using MinecraftProtocol.Packets.Client;
using NyaProxy.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy
{
    public class HandshakeEventArgs : TransportEventArgs, IHandshakeEventArgs
    {
        public HandshakePacket Packet { get; set; }

        public HandshakeEventArgs()
        {

        }
        public HandshakeEventArgs(HandshakePacket packet)
        {
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
        }
    }
}
