using MinecraftProtocol.IO;
using NyaProxy.API;
using NyaProxy.API.Channle;
using NyaProxy.API.Enum;
using NyaProxy.i18n;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy
{
    internal class Channle : IChannle
    {
        public string Name { get; }

        //这边读取的时候判断接口类型，如果是forge的就先读取第一个byte
        internal Dictionary<Guid, (IChannleMessage Handler, sbyte Discriminator)> MessageHandler;

        public Channle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
            MessageHandler = new ();
        }

        internal void Trigger(Side side, ByteReader reader, IBridge bridge)
        {
            try
            {
                foreach (var message in MessageHandler.Values)
                {
                    try
                    {
                        if((side & message.Handler.Side) == message.Handler.Side)
                        {
                            if(message.Handler is not IForgeChannleMessage)
                                message.Handler.ReadMessage(reader);
                            else if (message.Discriminator == reader.ReadByte())
                                message.Handler.ReadMessage(reader);
                            message.Handler.OnReceived(bridge);
                        }
                        reader.Reset();
                    }
                    catch (Exception e)
                    {
                        NyaProxy.Logger.Exception(e);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                //正常来说应该直接生成一个MesaageHandler.Values的副本防止遍历期间被修改，但有一些插件的数据包实在太频繁了，所以想避免复制和创建数组
                //但这样会带来安全问题，可能会导致有部分IChannleMessage没执行
            }
        }


        public Guid RegisterForgeMessage(IForgeChannleMessage handler, sbyte discriminator)
        {
            Guid id = Guid.NewGuid();
            MessageHandler.Add(id, (handler, discriminator));
            return id;
        }

        public Guid RegisterMessage(IChannleMessage handler)
        {
            Guid id = Guid.NewGuid();
            MessageHandler.Add(id, (handler, -1));
            return id;
        }

        public bool UnregisterMessage(Guid id)
        {
            if (MessageHandler.ContainsKey(id))
                return MessageHandler.Remove(id);
            else
                return false;
        }

        public void SendForgeMessage(IForgeChannleMessage handler, sbyte discriminator, Socket dest)
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteByte(discriminator);
            handler.WriteMessage(writer);
            BlockingBridge.Enqueue(dest, writer.AsMemory(), writer);
        }

        public void SendMessage(IChannleMessage handler, Socket dest)
        {
            ByteWriter writer = new ByteWriter();
            handler.WriteMessage(writer);
            BlockingBridge.Enqueue(dest, writer.AsMemory(), writer);
        }
    }
}
