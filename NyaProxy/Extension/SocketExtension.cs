using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Packets.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.Extension
{
    public static class SocketExtension
    {
        public static void DisconnectOnLogin(this Socket socket, string message, bool closeSocket = true) => DisconnectOnLogin(socket, new ChatMessage(message), closeSocket);
        public static void DisconnectOnLogin(this Socket socket, ChatMessage message, bool closeSocket = true)
        {
            if (closeSocket)
                BlockingBridge.Enqueue(socket, new DisconnectLoginPacket(message, -1).Pack(-1), socket);
            else
                BlockingBridge.Enqueue(socket, new DisconnectLoginPacket(message, -1).Pack(-1));
        }

        public static void DisconnectOnPlay(this Socket socket, string message, int protcolVersion, int compress, bool closeSocket = true) => DisconnectOnPlay(socket, new ChatMessage(message), protcolVersion, compress, closeSocket);
        public static void DisconnectOnPlay(this Socket socket, ChatMessage message, int protcolVersion, int compress, bool closeSocket = true)
        {
            if (closeSocket)
                BlockingBridge.Enqueue(socket, new DisconnectPacket(message, protcolVersion).Pack(compress), socket);
            else
                BlockingBridge.Enqueue(socket, new DisconnectPacket(message, protcolVersion).Pack(compress));
        }
    }
}
