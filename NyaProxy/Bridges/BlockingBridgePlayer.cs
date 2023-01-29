using MinecraftProtocol.DataType;
using System;
using NyaProxy.API;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Chat;

namespace NyaProxy.Bridges
{
    public class BlockingBridgePlayer : IPlayer
    {
        public BlockingBridge Own { get; set; }
        public UUID Id { get; set; }
        public string Name { get; set; }

        public BlockingBridgePlayer(BlockingBridge own, UUID id, string name)
        {
            Own = own;
            Id = id;
            Name = name;
        }

        public void Kick(ChatComponent reason)
        {
            BlockingBridge.Enqueue(Own.Source, new DisconnectPacket(reason, Own.ProtocolVersion).Pack(Own.ClientCompressionThreshold));
            Own.Break();
        }

        public void SendMessage(ChatComponent message, ChatPosition position = ChatPosition.ChatMessage)
        {
            BlockingBridge.Enqueue(Own.Source, new ServerChatMessagePacket(message.Serialize(), (byte)position, UUID.Empty, Own.ProtocolVersion).Pack(Own.ClientCompressionThreshold));
        }
    }
}
