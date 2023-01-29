using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MinecraftProtocol.Chat;
using NyaProxy.API;
using NyaProxy.API.Enum;

namespace NyaProxy
{
    public class AsyncChatEventArgs : CancelEventArgs, IAsyncChatEventArgs
    {
        public AsyncChatEventArgs()
        {

        }

        public AsyncChatEventArgs Setup(DateTime receivedTime , ChatComponent message)
        {
            _message = message;
            _receivedTime = receivedTime;
            return this;
        }


        public DateTime ReceivedTime => _receivedTime;
        private DateTime _receivedTime;

        public ChatComponent Message => _message;
        private ChatComponent _message;

    }
}
