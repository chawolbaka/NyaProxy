using MinecraftProtocol.Packets.Client;
using NyaProxy.API;
using System;

namespace NyaProxy
{
    public class LoginStartEventArgs : TransportEventArgs, ILoginStartEventArgs
    {
        public LoginStartPacket Packet { get; set; }

        public LoginStartEventArgs()
        {

        }
        public LoginStartEventArgs(LoginStartPacket packet)
        {
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
        }
    }
}
