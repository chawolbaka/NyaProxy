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
        Guid SessionId { get; }

        Socket Source { get; }
        Socket Destination { get; }
        
        //IPlayer Player { get; }
        //IServer Server { get; }

        IBridge Build();
        void Break();
    }
}
