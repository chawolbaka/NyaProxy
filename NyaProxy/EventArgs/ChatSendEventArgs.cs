using System;
using NyaProxy.API;
using NyaProxy.API.Enum;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;


namespace NyaProxy
{
    public class ChatSendEventArgs : PacketSendEventArgs, IChatSendEventArgs, IDisposable
    {
        public override CompatiblePacket Packet { get => base.Packet; set { base.Packet = value; _message = null; } }
        public virtual ChatComponent Message
        {
            get 
            { 
                if(_message == null) 
                {
                    switch (Direction)
                    {
                        case Direction.ToClient:
                            _definedPacket = Packet.AsServerChatMessage();
                            _message = (_definedPacket as ServerChatMessagePacket).Message; break;
                        case Direction.ToServer:
                            _definedPacket = Packet.AsClientChatMessage();
                            _message = ChatComponent.Parse((_definedPacket as ClientChatMessagePacket).Message); break;
                    }
                } 
                return _message;
            }
            set
            {
                if(_message is null)
                    throw new ArgumentNullException(nameof(value));

                switch (Direction)
                {
                    case Direction.ToClient:
                        using (ServerChatMessagePacket oldsPacket = _definedPacket as ServerChatMessagePacket)
                        {
                            ServerChatMessagePacket newsPacket = new ServerChatMessagePacket(value.Serialize(), oldsPacket.Position, oldsPacket.Sender, Packet.ProtocolVersion);
                            Packet = newsPacket.AsCompatible(Packet);
                            _definedPacket = newsPacket; break;
                        }
                    case Direction.ToServer:
                        using (ClientChatMessagePacket oldcPacket = _definedPacket as ClientChatMessagePacket)
                        {
                            ClientChatMessagePacket newcPacket = new ClientChatMessagePacket(value.ToString(), Packet.ProtocolVersion);
                            Packet = newcPacket.AsCompatible(Packet);
                            _definedPacket = newcPacket; break;
                        }
                }
                _message = value;
            }
        }
        private ChatComponent _message;
        private DefinedPacket _definedPacket;

        public void Dispose()
        {
            try
            {
                _definedPacket?.Dispose();
            }
            catch (Exception)
            {

            }
        }
    }
}
