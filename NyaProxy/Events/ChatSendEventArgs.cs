using System;
using NyaProxy.API;
using NyaProxy.API.Enum;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Compatible;
using NyaProxy.Bridges;
using System.Net.Sockets;
using MinecraftProtocol.IO;

namespace NyaProxy
{
    public class ChatSendEventArgs : PacketSendEventArgs, IChatSendEventArgs
    {
        public override LazyCompatiblePacket Packet { get => base.Packet; set { base.Packet = value; _message = null; } }
        public virtual ChatComponent Message
        {
            get 
            { 
                if(_message == null) 
                {
                    switch (Direction)
                    {
                        case Direction.ToClient:
                            CompatibleReader.TryReadServerChatMessage(Packet.Get(), Bridge.ChatTypes, out _message, out _definedPacket); break;
                        case Direction.ToServer:
                            if (CompatibleReader.TryReadClientChatMessage(Packet.Get(), out var message, out var ccmp))
                            {
                                _definedPacket = ccmp;
                                _message = ChatComponent.Parse(message);
                            }
                            else
                            {
                                throw new InvalidCastException($"Unknow chat packet {_definedPacket.Id}");
                            } break;
                    }
                } 
                return _message;
            }
            set
            {
                if (_message is null)
                    throw new ArgumentNullException(nameof(value));

                switch (Direction)
                {
                    case Direction.ToClient:
                        if (ProtocolVersion > ProtocolVersions.V1_19)
                        {
                            //不管原来是什么都直接转换成SystemChatMessage，否则还要处理签名和分离出来那堆属性太复杂了
                            SystemChatMessagePacket scmp = new SystemChatMessagePacket(_message.Serialize(), false, ProtocolVersion);
                            _definedPacket?.Dispose();
                            _definedPacket = scmp;
                            Packet = new FakeLazyCompatiblePacket(scmp.AsCompatible(Packet.Get()));
                        }
                        else if (_definedPacket is ServerChatMessagePacket scmp)
                        {
                            scmp.Context = value.Serialize();
                            Packet = new FakeLazyCompatiblePacket(_definedPacket.AsCompatible(Packet.Get()));
                        }
                        else
                        {
                            //一般来说不可能有其它选项，但以防未来修改读取的代码这边留个异常体系一下
                            throw new InvalidCastException($"Unknow chat packet {_definedPacket.Id}");
                        } break;
                    case Direction.ToServer:
                        if(_definedPacket is ClientChatMessagePacket ccmp)
                        {
                            ccmp.Message = value.ToString();
                            Packet = new FakeLazyCompatiblePacket(ccmp.AsCompatible(Packet.Get()));
                        }
                        else if(_definedPacket  is ChatCommandPacket ccp)
                        {
                            ccp.Command = value.ToString();
                            Packet = new FakeLazyCompatiblePacket(ccp.AsCompatible(Packet.Get()));
                        }
                        else
                        {
                            throw new InvalidCastException($"Unknow chat packet {_definedPacket.Id}");
                        } break;
                }
                _message = value;
            }
        }
        private ChatComponent _message;
        private DefinedPacket _definedPacket;

        internal override PacketSendEventArgs Setup(QueueBridge bridge, Socket source, Socket destination, Direction direction, CompatiblePacket packet, DateTime receivedTime)
        {
            _message = null;
            _definedPacket?.Dispose();
            _definedPacket = null;
            return base.Setup(bridge, source, destination, direction, packet, receivedTime);
        }
        internal override PacketSendEventArgs Setup(QueueBridge bridge, Socket source, Socket destination, Direction direction, PacketReceivedEventArgs e)
        {
            _message = null;
            _definedPacket?.Dispose();
            _definedPacket = null;
            return base.Setup(bridge, source, destination, direction, e);
        }
    }
}
