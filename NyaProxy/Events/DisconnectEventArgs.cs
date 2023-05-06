using MinecraftProtocol.Packets;
using NyaProxy.API;
using NyaProxy.API.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.EventArgs
{
    public class DisconnectEventArgs : CancelEventArgs, IDisconnectEventArgs
    {
        public long SessionId { get; }
        public IHost Host     { get; }

        public DisconnectEventArgs(long sessionId, IHost host)
        {
            SessionId = sessionId;
            Host = host;
        }
    }
}
