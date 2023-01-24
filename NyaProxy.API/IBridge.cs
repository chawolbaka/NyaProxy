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
        string HandshakeAddress { get; }
        Socket Source { get; }
        Socket Destination { get; }

        IBridge Build();
        void Break();
    }
}
