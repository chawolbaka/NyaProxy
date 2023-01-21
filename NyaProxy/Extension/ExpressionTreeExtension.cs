using MinecraftProtocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.Extension
{
    internal static class ExpressionTreeExtension
    {
        private static Func<Socket, EndPoint> GetField_Socket_localEndPoint = ExpressionTreeUtils.CreateGetFieldMethodFormInstance<Socket, EndPoint>("_localEndPoint");
        private static Func<Socket, EndPoint> GetField_Socket_remoteEndPoint = ExpressionTreeUtils.CreateGetFieldMethodFormInstance<Socket, EndPoint>("_remoteEndPoint");
        public static EndPoint _localEndPoint(this Socket socket) => GetField_Socket_localEndPoint(socket);
        public static EndPoint _remoteEndPoint(this Socket socket) => GetField_Socket_remoteEndPoint(socket);
    }
}
