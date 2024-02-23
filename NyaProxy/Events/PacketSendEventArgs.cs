using System;
using System.Net.Sockets;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Bridges;
using MinecraftProtocol.IO;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.IO.Pools;

namespace NyaProxy
{

    public class PacketSendEventArgs : TransportEventArgs, IPacketSendEventArgs, ICompatible
    {
        public long SessionId => Bridge.SessionId;

        public string Host => Bridge.Host.Name;

        public Socket Source { get; set; }
     
        public Socket Destination { get => _destination; set { DestinationCheaged = true; _destination = value; } }

        public virtual Direction Direction { get; private set; }

        public virtual DateTime ReceivedTime { get; private set; }

        public virtual Stage Stage => Bridge.Stage;

        public virtual int ProtocolVersion => Bridge.ProtocolVersion;

        public virtual int CompressionThreshold => Direction == Direction.ToClient ? Bridge.ClientCompressionThreshold : Bridge.ServerCompressionThreshold;

        public IPlayer Player => Bridge.Player;

        public bool DestinationCheaged { get; private set; }

        public bool PacketCheaged => _packetCheaged ||  _version != _packet.Version; //这样计算不准确，如果数据包被压缩就会导致该值变大，但无所谓，反正我也不需要非常准确的去统计
       
        public int BytesTransferred => _bytesTransferred <= 0 ||  PacketCheaged ? _bytesTransferred = VarInt.GetLength(Packet.Get().Count) + Packet.Get().Count : _bytesTransferred;

        public virtual LazyCompatiblePacket Packet
        {
            get => _packet; 
            set 
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                _packet?.Dispose();
                _packet = value; 
                _packetCheaged = true; 
            }
        }

        private Socket _destination;
        private LazyCompatiblePacket _packet;
        private bool _packetCheaged;
        private int _version;
        private int _bytesTransferred;


        internal QueueBridge Bridge;
        internal PacketReceivedEventArgs EventArgs;
        

        public PacketSendEventArgs()
        {
            _bytesTransferred = -1;
        }

        internal virtual PacketSendEventArgs Setup(QueueBridge bridge, Socket source, Socket destination, Direction direction, CompatiblePacket packet, DateTime receivedTime)
        {
            return Setup(bridge, source, destination, direction, new FakeLazyCompatiblePacket(packet), receivedTime);
        }
        internal virtual PacketSendEventArgs Setup(QueueBridge bridge, Socket source, Socket destination, Direction direction, LazyCompatiblePacket packet, DateTime receivedTime)
        {
            if(destination == null)
                throw new ArgumentNullException(nameof(destination));

            Source = source;
            Direction = direction;
            Destination = destination;
            Bridge = bridge;
            EventArgs = null;
            DestinationCheaged = false;
            ReceivedTime = receivedTime;
            _packet = packet;
            _packetCheaged = false;
            _bytesTransferred = 0;
            _version = packet.Version;
            _isBlock = false;
            _isCancelled = false;
            return this;
        }
        internal virtual PacketSendEventArgs Setup(QueueBridge bridge, Socket source, Socket destination, Direction direction, PacketReceivedEventArgs e)
        {
            if(e == null)
                throw new ArgumentNullException(nameof(e));

            Setup(bridge, source, destination, direction, e.Packet, e.ReceivedTime);
            EventArgs = e;
            _bytesTransferred = e.PacketLength;
            return this;
        }
    }
}
