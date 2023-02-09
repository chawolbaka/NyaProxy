﻿using System;
using NyaProxy.API;
using NyaProxy.API.Enum;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Compatible;

namespace NyaProxy
{
    public class ChatSendEventArgs : PacketSendEventArgs, IChatSendEventArgs, IDisposable
    {
        public override CompatiblePacket Packet { get => base.Packet; set { base.Packet = value; _message = null; } }
        public virtual ChatComponent Message
        {
            get 
            { 
                if(_message == null) 
                {
                    switch (Direction)
                    {
                        case Direction.ToClient:
                            CompatibleReader.TryReadChatMessage(Packet, out _message, out _definedPacket); break;
                        case Direction.ToServer:
                            if(ClientChatMessagePacket.TryRead(Packet, out var ccmp))
                            {
                                _definedPacket = ccmp;
                                _message = ChatComponent.Parse(ccmp.Message);
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
                            Packet = scmp.AsCompatible(Packet);
                        }
                        else if (_definedPacket is ServerChatMessagePacket scmp)
                        {
                            scmp.Context = value.Serialize();
                            Packet = _definedPacket.AsCompatible(Packet);
                        }
                        else
                        {
                            //一般来说不可能有其它选项，但以防未来修改读取的代码这边留个异常体系一下
                            throw new InvalidCastException($"Unknow chat packet {_definedPacket.Id}");
                        } break;
                    case Direction.ToServer:
                        ClientChatMessagePacket ccmp = _definedPacket as ClientChatMessagePacket;
                        ccmp.Message = value.ToString();
                        Packet = ccmp.AsCompatible(Packet); break;
                }
                _message = value;
            }
        }
        private ChatComponent _message;
        private DefinedPacket _definedPacket;

        public void Dispose()
        {
            try
            {
                _definedPacket?.Dispose();
            }
            catch (Exception)
            {

            }
        }
    }
}
