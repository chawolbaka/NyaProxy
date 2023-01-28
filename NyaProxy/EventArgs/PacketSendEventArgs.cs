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

namespace NyaProxy
{

    public class PacketSendEventArgs : TransportEventArgs, IPacketSendEventArgs
    {
        public Socket Destination { get; set; }

        public virtual Direction Direction { get; private set; }
        
        public virtual DateTime ReceivedTime => _eventArgs.ReceivedTime;

        public virtual int ProtocolVersion => _bridge.ProtocolVersion;

        public IPlayer Player => _bridge.Player;

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

        internal bool PacketCheaged => _packetCheaged || _version != GetField_ByteWriter_versionn(_packet);

        
        private static Func<ByteWriter, int> GetField_ByteWriter_versionn = ExpressionTreeUtils.CreateGetFieldMethodFormInstance<ByteWriter, int>("_version");
        private CompatiblePacket _packet;
        private bool _packetCheaged;
        private int _version;
        
        internal BlockingBridge _bridge;
        internal PacketReceivedEventArgs _eventArgs;


        public PacketSendEventArgs()
        {

        }

        internal PacketSendEventArgs Setup(BlockingBridge bridge, Socket destination, Direction direction, PacketReceivedEventArgs e)
        {
            Direction = direction;
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
            _eventArgs = e ?? throw new ArgumentNullException(nameof(e));
            _packetCheaged = false;
            _version = GetField_ByteWriter_versionn(e.Packet);
            _packet = e.Packet;
            _bridge = bridge;
            _isBlock = false;
            _isCancelled = false;
            return this;
        }
    }
}
