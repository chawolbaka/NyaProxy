using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using NyaProxy.API;

namespace Keepalive
{
    public class KeepalivePlugin : NyaPlugin
    {
        public PluginConfig Config => Helper.Config.Get<PluginConfig>(0);

        public Dictionary<UUID, DateTime> LastKeepAlive = new Dictionary<UUID, DateTime>();

        public static string KickReasonJson = new ChatComponent { Translate = "disconnect.timeout" }.Serialize();

        public override async Task OnEnable()
        {
            Helper.Config.Register(typeof(PluginConfig));
            Helper.Events.Transport.PacketSendToServer += OnPacketSendToServer;
            Helper.Events.Transport.PacketSendToClient += OnPacketSendToClient;
        }

        private void OnPacketSendToServer(object sender, IPacketSendEventArgs e)
        {
            if (e.Packet == PacketType.Play.Client.KeepAlive)
            {
                //如果Timeout是负数就无限接管，客户端永远不会因为连接超时掉线
                if (Config.Timeout > 0 && e.Player != null)
                {
                    UUID id = e.Player.Id;
                    if (!LastKeepAlive.ContainsKey(id))
                    {
                        LastKeepAlive.Add(id, DateTime.Now);
                    }
                    else if ((DateTime.Now - LastKeepAlive[id]).TotalMilliseconds > Config.Timeout)
                    {
                        e.Player?.KickAsync(KickReasonJson);
                    }
                    else
                    {
                        LastKeepAlive[id] = DateTime.Now;
                    }
                }

                e.Block();
            }
        }

        private void OnPacketSendToClient(object sender, IPacketSendEventArgs e)
        {
            if (e.Packet == PacketType.Play.Server.KeepAlive)
            {
                //直接由代理端发送给服务端响应包，但依旧会发送给客户端，不过客户端那边就别想发到服务端了，这么做的目的是重写心跳包的超时时间
                e.Packet.Id = KeepAliveResponsePacket.GetPacketId(e.ProtocolVersion);
                e.Destination = e.Source;
            }
        }

        public override async Task OnDisable()
        {
            Helper.Events.Transport.PacketSendToServer -= OnPacketSendToServer;
            Helper.Events.Transport.PacketSendToClient -= OnPacketSendToClient;
        }

    }
}
