using MinecraftProtocol.DataType;
using MinecraftProtocol.DataType.Chat;
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


        void SendMessage(ChatMessage message, ChatPosition position = ChatPosition.ChatMessage);
        void Kick(ChatMessage reason);
    }
}
