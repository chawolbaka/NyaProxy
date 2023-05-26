using System.Net;
using NyaProxy.API;

namespace Analysis
{

    public class SessionAnalysisData : BridgeAnalysisData
    {
        public DateTime LoginStartTime { get; set; }
        
        public DateTime LoginSuccessTime { get; set; }
        
        public IPlayer Player { get; set; }
     
        public (PacketAnalysisData Client, PacketAnalysisData Server) PacketAnalysis { get; set; }
     
        public SessionAnalysisData()
        {
            PacketAnalysis = (new PacketAnalysisData(), new PacketAnalysisData());
        }
    }
}