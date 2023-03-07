using System;
using NyaProxy.API;
using MinecraftProtocol.Chat;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Compatible;
using System.Threading.Tasks;

namespace NyaProxy.Bridges
{
    public class BlockingBridgePlayer : IPlayer
    {
        public BlockingBridge Own { get; }
        public UUID Id { get; }
        public string Name { get; }

        public BlockingBridgePlayer(BlockingBridge own, UUID id, string name)
        {
            Own = own;
            Id = id;
            Name = name;
        }

        /// <summary>
        /// 踢掉该玩家
        /// </summary>
        /// <param name="reason">踢掉的原因</param>
        public Task KickAsync(ChatComponent reason)
        {
            TaskCompletionSource completionSource = new TaskCompletionSource();

            //不用考虑DisconnectLoginPacket，因为按现在的设计玩家被初始化的时候必定是已经加入服务器了的状态
            Packet packet = new DisconnectPacket(reason, Own.ProtocolVersion);
            NyaProxy.Network.Enqueue(Own.Source, Own.CryptoHandler.TryEncrypt(packet.Pack(Own.ClientCompressionThreshold)), () => 
            {
                Own.Break();
                packet?.Dispose(); 
                completionSource.SetResult();
            });
            return completionSource.Task;
        }

        /// <summary>
        /// 对该玩家发送聊天消息
        /// </summary>
        public Task SendMessageAsync(ChatComponent message, ChatPosition position = ChatPosition.ChatMessage)
        {
            TaskCompletionSource completionSource = new TaskCompletionSource();
            Packet packet = Own.BuildServerChatMessage(message.Serialize(), position);
            NyaProxy.Network.Enqueue(Own.Source, Own.CryptoHandler.TryEncrypt(packet.Pack(Own.ClientCompressionThreshold)), () => 
            {
                packet?.Dispose();
                completionSource.SetResult();
            });
            return completionSource.Task;
        }
    }
}
