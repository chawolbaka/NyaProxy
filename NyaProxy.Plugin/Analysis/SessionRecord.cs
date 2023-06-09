using System.Net;
using NyaProxy.API;

namespace Analysis
{
    public class SessionRecord : BridgeRecord
    {
        public int ProtocolVersion { get; set; }

        public int ClientCompressionThreshold { get; set; }

        public int ServerCompressionThreshold { get; set; }

        public DateTime LoginStartTime { get; set; }
        
        public DateTime LoginSuccessTime { get; set; }
        
        public IPlayer Player { get; set; }
     
        public (PacketRecord Client, PacketRecord Server) PacketAnalysis { get; set; }
     
        public SessionRecord()
        {
            PacketAnalysis = (new PacketRecord(), new PacketRecord());
        }
    }
}