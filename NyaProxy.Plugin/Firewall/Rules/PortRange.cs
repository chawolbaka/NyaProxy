namespace Firewall.Rules
{
    public class PortRange : IEquatable<int>, IEquatable<PortRange>
    {
        private static readonly string Delimiter = "-";

        public ushort Start;
        public ushort End;

        public PortRange(ushort port)
        {
            Start = port;
            End = port;
        }

        public PortRange(ushort start, ushort end)
        {
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
                return new PortRange(ushort.Parse(str.AsSpan().Slice(0, index)), ushort.Parse(str.AsSpan().Slice(index + Delimiter.Length)));

        }

        public bool Equals(int port)
        {
            return port >= Start && port <= End;
        }

        public bool Equals(PortRange? other)
        {
            return other != null && other.Start == Start && other.End == End;
        }

        public override bool Equals(object? obj)
        {
            return obj != null && Equals(obj as PortRange);
        }

        public override int GetHashCode()
        {
            return Start & End << 8;
        }

        public override string ToString()
        {
            return Start == End ? Start.ToString() : $"{Start}{Delimiter}{End}";
        }
    }
}