namespace Analysis
{
    public class PacketAnalysis : Analysis
    {
        public long BytesTransferred { get; set; }
        public Dictionary<int, TransportAnalysis> Table { get; set; }

        public PacketAnalysis()
        {
            Table = new Dictionary<int, TransportAnalysis>();
        }
    }
}