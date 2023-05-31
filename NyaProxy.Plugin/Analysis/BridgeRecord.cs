using System.Net;
using NyaProxy.API;

namespace Analysis
{
    public class BridgeRecord
    {
        public long SessionId { get; set; }
        public DateTime ConnectTime { get; set; }
        public DateTime HandshakeTime { get; set; }
        public DateTime DisconnectTime { get; set; }
        public IPEndPoint Source { get; set; }
        public IPEndPoint Destination { get; set; }
        public IHost Host { get; set; }
    }
}