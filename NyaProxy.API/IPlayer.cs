using MinecraftProtocol.Chat;
using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public interface IPlayer
    {
        /// <summary>
        /// 玩家的UUID
        /// </summary>
        UUID Id { get; }

        /// <summary>
        /// 玩家名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 对该玩家发送聊天消息
        /// </summary>
        Task SendMessageAsync(ChatComponent message, ChatPosition position = ChatPosition.ChatMessage);


        /// <summary>
        /// 踢掉该玩家
        /// </summary>
        /// <param name="reason">踢掉的原因</param>
        Task KickAsync(string reason);

        /// <summary>
        /// 踢掉该玩家
        /// </summary>
        /// <param name="reason">踢掉的原因</param>
        Task KickAsync(ChatComponent reason);

    }
}
