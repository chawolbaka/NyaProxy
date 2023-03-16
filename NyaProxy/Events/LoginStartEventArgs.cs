using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets.Client;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Bridges;
using System;
using System.Net.Sockets;

namespace NyaProxy
{
    public class LoginStartEventArgs : PacketSendEventArgs, ILoginStartEventArgs
    {

        public string PlayerName { get; set; }

        public UUID PlayerUUID { get; set; }

        public LoginStartEventArgs(BlockingBridge bridge, Socket source, Socket destination, Direction direction, LoginStartPacket packet, DateTime receivedTime)
        {
            Setup(bridge, source, destination, direction, packet.AsCompatible(bridge.ProtocolVersion, bridge.ClientCompressionThreshold), receivedTime);
            PlayerName = packet.PlayerName;
            PlayerUUID = packet.PlayerUUID;
        }
    }
}
