using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MinecraftProtocol.DataType.Chat;
using NyaProxy.API;
using NyaProxy.API.Enum;

namespace NyaProxy
{
    public class AsyncChatEventArgs : CancelEventArgs, IAsyncChatEventArgs
    {
        public AsyncChatEventArgs()
        {

        }

        public AsyncChatEventArgs Setup(IServer server, DateTime receivedTime , ChatMessage message)
        {
            _server = server;
            _message = message;
            _receivedTime = receivedTime;
            return this;
        }


        public DateTime ReceivedTime => _receivedTime;
        private DateTime _receivedTime;

        public IServer Server => _server;
        private IServer _server;

        public ChatMessage Message => _message;
        private ChatMessage _message;

    }
}
