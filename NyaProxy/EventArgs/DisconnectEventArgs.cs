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
        public string Host { get; set; }

        public DisconnectEventArgs(string host)
        {
            Host = host;
        }
    }
}
