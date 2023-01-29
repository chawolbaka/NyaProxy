using MinecraftProtocol.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public interface IChatSendEventArgs : IPacketSendEventArgs
    {
        ChatComponent Message { get; set; }
    }
}
