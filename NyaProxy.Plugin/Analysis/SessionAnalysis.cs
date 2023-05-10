using System.Net;
using NyaProxy.API;

namespace Analysis
{
    public class SessionAnalysis
    {
        public long SessionId { get; set; }
        public DateTime ConnectTime { get; set; }
        public DateTime HandshakeTime { get; set; }
        public DateTime LoginStartTime { get; set; }
        public DateTime LoginSuccessTime { get; set; }
        public DateTime DisconnectTime { get; set; }
        public IPEndPoint Source { get; set; }
        public IPEndPoint Destination { get; set; }
        public IHost Host { get; set; }
        public IPlayer Player { get; set; }
        public (PacketAnalysis Client, PacketAnalysis Server) PacketAnalysis { get; set; }
     
        public SessionAnalysis()
        {
            PacketAnalysis = (new PacketAnalysis(), new PacketAnalysis());
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
                SizeSuffix(PacketAnalysis.Client.BytesTransferred+PacketAnalysis.Server.BytesTransferred),
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

        //By: https://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc
        private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private static string SizeSuffix(long value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}