namespace Analysis
{
    public class PacketAnalysisData : BaseAnalysisData
    {
        public long BytesTransferred { get; set; }
        public Dictionary<int, TransportAnalysisData> Table { get; set; }

        public PacketAnalysisData()
        {
            Table = new Dictionary<int, TransportAnalysisData>();
        }
    }
}