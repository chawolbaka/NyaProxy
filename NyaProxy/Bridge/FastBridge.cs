using NyaProxy.API;
using System.Net.Sockets;
using System.Threading;

namespace NyaProxy
{
    public class FastBridge : Bridge
    {
        public FastBridge(Socket source, Socket destination) : base(source, destination)
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
