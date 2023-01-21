using MinecraftProtocol.DataType;
using System;
using NyaProxy.API;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Packets.Server;

namespace NyaProxy
{
    public class BlockingBridgePlayer : IPlayer
    {
        public BlockingBridge Own { get; set; }

        public string Name { get; set; }
        public UUID Id { get; set; }

        public BlockingBridgePlayer(BlockingBridge own, UUID id, string name)
        {
            Own = own;
            Id = id; 
            Name = name;
        }

        public void Kick(ChatMessage reason)
        {
            BlockingBridge.Enqueue(Own.Source, new DisconnectPacket(reason, Own.ProtocolVersion).Pack(Own.CompressionThreshold));
            Own.Break();
        }

        public void SendMessage(ChatMessage message, ChatPosition position = ChatPosition.ChatMessage)
        {
            BlockingBridge.Enqueue(Own.Source, new ServerChatMessagePacket(message.Serialize(), (byte)position, UUID.Empty, Own.ProtocolVersion).Pack(Own.CompressionThreshold));
        }
    }
}
