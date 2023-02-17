using MinecraftProtocol.Packets;
using MinecraftProtocol.IO.Extensions;
using NyaProxy.API.Enum;

namespace NyaProxy.EventArgs
{
    public class PluginChannleSendEventArgs : PacketSendEventArgs
    {
        public string ChannleName { get { InitDefinedPacket(); return _channleName; } }
        public byte[] Data        { get { InitDefinedPacket(); return _data; } }

        private string _channleName;
        private byte[] _data;
        private DefinedPacket _definedPacket;

        private void InitDefinedPacket()
        {
            if(_definedPacket is null)
            {
                switch (Direction)
                {
                    case Direction.ToClient:
                        var spcp = Packet.AsServerPluginChannel(Bridge.IsForge);
                        _channleName = spcp.Channel;
                        _data = spcp.Data;
                        _definedPacket = spcp; break;
                    case Direction.ToServer:
                        var cpcp = Packet.AsClientPluginChannel(Bridge.IsForge);
                        _channleName = cpcp.Channel;
                        _data = cpcp.Data;
                        _definedPacket = cpcp; break;
                }
            }


        }

    }
}
