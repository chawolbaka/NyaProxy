using NyaGenerator.Equatable;

namespace Firewall.Rules
{
    [Equatable]
    public partial class PortRange : IEquatable<int>
    {
        private static readonly string Delimiter = "-";

        public ushort Start { get; set; }
        public ushort End { get; set; }

        public PortRange(ushort port)
        {
            Start = port;
            End = port;
        }

        public PortRange(ushort start, ushort end)
        {
            if (end < start)
                throw new ArgumentOutOfRangeException(nameof(end));

            Start = start;
            End = end;
        }

        public static PortRange Parse(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentNullException(nameof(str));

            int index = str.IndexOf(Delimiter);
            if (index == -1)
                return new PortRange(ushort.Parse(str), ushort.Parse(str));
            else
                return new PortRange(ushort.Parse(str.AsSpan(0, index)), ushort.Parse(str.AsSpan(index + Delimiter.Length)));

        }

        public bool Equals(int port)
        {
            return port >= Start && port <= End;
        }

        public override string ToString()
        {
            return Start == End ? Start.ToString() : $"{Start}{Delimiter}{End}";
        }
    }
}