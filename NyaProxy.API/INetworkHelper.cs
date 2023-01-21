﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Packets;

namespace NyaProxy.API
{
    public interface INetworkHelper
    {
        //不使用IPacket是防止遗忘填写CompressionThreshold
        void Enqueue(Socket socket, ICompatiblePacket packet);
        void Enqueue(Socket socket, Memory<byte> data);

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="socket">目标连接</param>
        /// <param name="data">发送的数据</param>
        /// <param name="disposable">发送完成后调用</param>
        void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable);
        //void Kick(Guid sessionId, ChatMessage reason);
    }
}
