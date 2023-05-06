using NyaProxy.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy
{
    public class ConnectEventArgs : TransportEventArgs, IConnectEventArgs
    {
        public long SessionId { get; }
        public Socket AcceptSocket { get; set; }
        public SocketError SocketError { get; }

        public ConnectEventArgs()
        {

        }

        public ConnectEventArgs(long sessionId, Socket acceptSocket)
        {
            SessionId = sessionId;
            AcceptSocket = acceptSocket ?? throw new ArgumentNullException(nameof(acceptSocket));
        }

        public ConnectEventArgs(long sessionId, Socket acceptSocket, SocketError socketError) : this(sessionId, acceptSocket)
        {
            SocketError = socketError;
        }
    }
}
