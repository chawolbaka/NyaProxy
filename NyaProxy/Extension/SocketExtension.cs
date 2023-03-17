using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NyaProxy.Bridges;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Packets.Server;
using System.Runtime.InteropServices;
using System.Net;

namespace NyaProxy.Extension
{
    public static class SocketExtension
    {
        public static void DisconnectOnLogin(this Socket socket, string message, bool closeSocket = true)
        {
            NyaProxy.Network.Enqueue(socket, PacketCache.DisconnectLogin.Get(message), closeSocket ? socket : null);
        }
        
        public static void DisconnectOnPlay(this Socket socket, string message, int protcolVersion, int compress, bool closeSocket = true)
        {
            NyaProxy.Network.Enqueue(socket, PacketCache.Disconnect.Get(message), closeSocket ? socket : null);
        }

        public static void DisconnectOnLogin(this Socket socket, ChatComponent message, bool closeSocket = true)
        {
            NyaProxy.Network.Enqueue(socket, new DisconnectLoginPacket(message, -1).Pack(-1), closeSocket ? socket : null);
        }

        public static void DisconnectOnPlay(this Socket socket, ChatComponent message, int protcolVersion, int compress, bool closeSocket = true)
        {
            NyaProxy.Network.Enqueue(socket, new DisconnectPacket(message, protcolVersion).Pack(compress), closeSocket ? socket : null);
        }


        //By: https://blog.hjc.im/dotnet-core-tcp-fast-open.html
        [DllImport("libc.so.6", SetLastError = true)]
        private static extern unsafe int setsockopt(int sockfd, int level, int optname, void* optval, int optlen);

        public static unsafe void EnableLinuxFastOpenServer(this Socket socket)
        {
            const int TCP_FASTOPEN_LINUX = 23;
            int qlen = 5;
            int result = setsockopt(
                (int)socket.Handle,
                6 /* SOL_TCP */,
                TCP_FASTOPEN_LINUX,
                &qlen, sizeof(int));
            if (result == -1)
            {
                throw new SocketException(Marshal.GetLastWin32Error());
            }
        }
        
        public static unsafe void EnableLinuxFastOpenConnect(this Socket socket)
        {
            const int TCP_FASTOPEN_CONNECT = 30;
            int val = 1;
            int result = setsockopt(
                (int)socket.Handle,
                6 /*SOL_TCP*/,
                TCP_FASTOPEN_CONNECT,
                &val, sizeof(int));
            if (result == -1)
            {
                throw new SocketException(Marshal.GetLastWin32Error());
            }
        }


        public static void EnableWindowsFastOpenClient(this Socket socket)
        {
            const int TCP_FASTOPEN_WINDOWS = 15;
            socket.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)TCP_FASTOPEN_WINDOWS, 1);
        }
    }
}
