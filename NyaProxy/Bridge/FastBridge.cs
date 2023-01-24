using NyaProxy.API;
using System.Net.Sockets;
using System.Threading;

namespace NyaProxy
{
    public class FastBridge : Bridge
    {
        public FastBridge(HostConfig host, string handshakeAddress, Socket source, Socket destination) : base(host, handshakeAddress, source, destination)
        {
        }

        public override Bridge Build()
        {
            CancellationTokenSource cancellation =  new CancellationTokenSource();
            cancellation.Token.Register(Break);
            TransportLayerRepeater.Create(Source, Destination, cancellation);

            return this;
        }
    }
}
