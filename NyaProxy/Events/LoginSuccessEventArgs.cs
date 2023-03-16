using MinecraftProtocol.DataType;

using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Bridges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy
{
    public class LoginSuccessEventArgs : PacketSendEventArgs, ILoginSuccessEventArgs
    {
        public override CompatiblePacket Packet { get => base.Packet; set { base.Packet = value; _loginSuccessPacket = null; } }
        public UUID Id
        {
            get
            {
                InitPacket();
                return _loginSuccessPacket.PlayerUUID;
            }
            set
            {
                InitPacket();
                _loginSuccessPacket.PlayerUUID = value;
            }
        }
        public string Name
        {
            get
            {
                InitPacket();
                return _loginSuccessPacket.PlayerName;
            }
            set
            {
                InitPacket();
                _loginSuccessPacket.PlayerName = value;
            }
        }

        private LoginSuccessPacket _loginSuccessPacket;
        private void InitPacket()
        {
            if (_loginSuccessPacket is not null)
                return;
            _loginSuccessPacket = Packet.AsLoginSuccess();
        }


    }
}
