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
        UUID Id { get; }
        string Name { get; }

        void SendMessage(ChatMessage message, ChatPosition position = ChatPosition.ChatMessage);
        void Kick(ChatMessage reason);
    }
}
