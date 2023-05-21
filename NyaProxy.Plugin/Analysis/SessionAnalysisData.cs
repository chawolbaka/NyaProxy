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

        public List<object> ToRow(bool hideTime)
        {
            List<object> row = new List<object>
            {
                SessionId,
                Host != null ? Host.Name : "",
                Player != null ? Player.Name : "",
                Source != null ? Source.ToString() : "",
                Destination != null ? Destination.ToString() : "",
                Utils.SizeSuffix(PacketAnalysis.Client.BytesTransferred+PacketAnalysis.Server.BytesTransferred),
            };

            if (!hideTime)
            {
                row.AddRange(new object[] {
                ConnectTime      != default ? ConnectTime      : "",
                HandshakeTime    != default ? HandshakeTime    : "",
                LoginStartTime   != default ? LoginStartTime   : "",
                LoginSuccessTime != default ? LoginSuccessTime : "",
                DisconnectTime   != default ? DisconnectTime   : ""
                });
            }
            return row;
        }

    }
}