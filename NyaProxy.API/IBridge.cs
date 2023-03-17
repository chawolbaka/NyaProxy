using MinecraftProtocol.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public interface IBridge
    {
        long SessionId { get; }
        Socket Source { get; }
        Socket Destination { get; }

        void Break();
    }
}
