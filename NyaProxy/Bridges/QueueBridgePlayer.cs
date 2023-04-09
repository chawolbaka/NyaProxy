using System;
using NyaProxy.API;
using MinecraftProtocol.Chat;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Compatible;
using System.Threading.Tasks;
using NyaProxy.API.Enum;

namespace NyaProxy.Bridges
{
    public class QueueBridgePlayer : IPlayer
    {
        public QueueBridge Own { get; internal set; }
        public UUID Id { get; internal set; }
        public string Name { get; internal set; }

        public QueueBridgePlayer(QueueBridge own, UUID id, string name)
        {
            Own = own;
            Id = id;
            Name = name;
        }

        public Task KickAsync(string reason)
        {
            TaskCompletionSource completionSource = new TaskCompletionSource();
            byte[] packet = Own.Stage == Stage.Play ? PacketCache.GetDisconnect(reason, Own.ProtocolVersion, Own.ServerCompressionThreshold) : PacketCache.GetDisconnectLogin(reason);
            NyaProxy.Network.Enqueue(Own.Source, Own.CryptoHandler.TryEncrypt(packet), () =>
            {
                Own.Break();
                completionSource.SetResult();
            });
            return completionSource.Task;
        }

        public Task KickAsync(ChatComponent reason)
        {
            Packet packet = Own.Stage == Stage.Play ? new DisconnectPacket(reason, Own.ProtocolVersion) : new DisconnectLoginPacket(reason, -1);
            TaskCompletionSource completionSource = new TaskCompletionSource();
            NyaProxy.Network.Enqueue(Own.Source, Own.CryptoHandler.TryEncrypt(packet.Pack(Own.ServerCompressionThreshold)), () =>
            {
                Own.Break();
                packet?.Dispose();
                completionSource.SetResult();
            });
            return completionSource.Task;
        }

        public Task SendMessageAsync(ChatComponent message, ChatPosition position = ChatPosition.ChatMessage)
        {
            if (Own.Stage != Stage.Play)
                throw new NotSupportedException();

            TaskCompletionSource completionSource = new TaskCompletionSource();
            Packet packet = Own.BuildServerChatMessage(message.Serialize(), position);
            NyaProxy.Network.Enqueue(Own.Source, Own.CryptoHandler.TryEncrypt(packet.Pack(Own.ServerCompressionThreshold)), () => 
            {
                packet?.Dispose();
                completionSource.SetResult();
            });
            return completionSource.Task;
        }
    }
}
