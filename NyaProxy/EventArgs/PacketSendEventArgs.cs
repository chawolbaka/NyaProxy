using MinecraftProtocol.IO;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Utils;
using NyaProxy.API;
using NyaProxy.API.Enum;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NyaProxy.Bridges;
using MinecraftProtocol.Compatible;

namespace NyaProxy
{

    public class PacketSendEventArgs : TransportEventArgs, IPacketSendEventArgs, ICompatible
    {
        public Socket Destination { get; set; }

        public virtual Direction Direction { get; private set; }

        public virtual DateTime ReceivedTime { get; private set; }

        public virtual int ProtocolVersion => Bridge.ProtocolVersion;

        public IPlayer Player => Bridge.Player;

        public bool PacketCheaged => _packetCheaged || _version != GetField_ByteWriter_versionn(_packet);

        public virtual CompatiblePacket Packet 
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

        private static Func<ByteWriter, int> GetField_ByteWriter_versionn = ExpressionTreeUtils.CreateGetFieldMethodFormInstance<ByteWriter, int>("_version");
        private CompatiblePacket _packet;
        private bool _packetCheaged;
        private int _version;
        
        internal BlockingBridge Bridge;
        internal PacketReceivedEventArgs EventArgs;


        public PacketSendEventArgs()
        {

        }

        internal virtual PacketSendEventArgs Setup(BlockingBridge bridge, Socket destination, Direction direction, CompatiblePacket packet, DateTime receivedTime)
        {
            if(destination == null)
                throw new ArgumentNullException(nameof(destination));

            Direction = direction;
            Destination = destination;
            Bridge = bridge;
            EventArgs = null;
            _packet = packet;
            _packetCheaged = false;
            _version = GetField_ByteWriter_versionn(packet);
            _isBlock = false;
            _isCancelled = false;
            return this;
        }
        internal virtual PacketSendEventArgs Setup(BlockingBridge bridge, Socket destination, Direction direction, PacketReceivedEventArgs e)
        {
            if(e == null)
                throw new ArgumentNullException(nameof(e));

            Setup(bridge, destination, direction, e.Packet, e.ReceivedTime);
            EventArgs = e;
            return this;
        }
    }
}
