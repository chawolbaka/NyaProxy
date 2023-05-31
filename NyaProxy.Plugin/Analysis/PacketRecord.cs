namespace Analysis
{
    public class PacketRecord : BaseRecord
    {
        public long BytesTransferred { get; set; }
        public Dictionary<int, TransportRecord> Table { get; set; }

        public PacketRecord()
        {
            Table = new Dictionary<int, TransportRecord>();
        }
    }
}