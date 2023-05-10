using System;
using System.Net;
using System.Text.Json;
using NyaProxy.API;
using MinecraftProtocol.Packets.Client;

namespace Analysis
{

    public class AnalysisPlgin : NyaPlugin
    {
        //还需要各个IP连接数的统计和SLP的
        public static Dictionary<long, SessionAnalysis> Sessions = new ();
        private DateTime _startTime;

        public override async Task OnEnable()
        {
            _startTime = DateTime.Now;
            Helper.Events.Transport.Connecting   += Transport_Connecting;
            Helper.Events.Transport.Disconnected += Transport_Disconnected;
            Helper.Events.Transport.Handshaking  += Transport_Handshaking;
            Helper.Events.Transport.LoginStart   += Transport_LoginStart;
            Helper.Events.Transport.LoginSuccess += Transport_LoginSuccess;
            Helper.Events.Transport.PacketSendToServer += Transport_PacketSendToServer;
            Helper.Events.Transport.PacketSendToClient += Transport_PacketSendToClient;
            Helper.Command.Register(new OverviewCommand());
        }


        private void Transport_Connecting(object? sender, IConnectEventArgs e)
        {
            SessionAnalysis anlysis = new SessionAnalysis();
            anlysis.SessionId = e.SessionId;
            anlysis.ConnectTime = DateTime.Now;
            anlysis.Source = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            Sessions.Add(e.SessionId, anlysis);
        }

        private void Transport_Handshaking(object? sender, IHandshakeEventArgs e)
        {
            if(e.Packet.NextState == HandshakeState.Login)
            {
                SessionAnalysis anlysis = Sessions[e.SessionId];
                anlysis.Host = e.Host;
                anlysis.HandshakeTime = DateTime.Now;
            }
            else
            {
                Sessions.Remove(e.SessionId);
            }
        }

        private void Transport_LoginStart(object? sender, ILoginStartEventArgs e)
        {
            SessionAnalysis anlysis = Sessions[e.SessionId];
            anlysis.Player = e.Player;
            anlysis.LoginStartTime = DateTime.Now;
            anlysis.Destination = e.Destination.RemoteEndPoint as IPEndPoint;
        }

        private void Transport_LoginSuccess(object? sender, ILoginSuccessEventArgs e)
        {
            Sessions[e.SessionId].LoginSuccessTime = DateTime.Now;
        }

        private void Transport_PacketSendToClient(object? sender, IPacketSendEventArgs e)
        {
            if (Sessions.TryGetValue(e.SessionId, out var anlysis))
                Add(anlysis.PacketAnalysis.Server, e);
        }

        private void Transport_PacketSendToServer(object? sender, IPacketSendEventArgs e)
        {
            if (Sessions.TryGetValue(e.SessionId, out var anlysis))
                Add(anlysis.PacketAnalysis.Client, e);
        }

        private void Add(PacketAnalysis anslysis, IPacketSendEventArgs e)
        {
            int id = e.Packet.Id;

            if (!anslysis.Table.ContainsKey(id))
                anslysis.Table.Add(id, new TransportAnalysis());

            anslysis.Table[id].Count++;
            anslysis.Table[id].BytesTransferred += e.BytesTransferred;

            anslysis.Count++;
            anslysis.BytesTransferred += e.BytesTransferred;
        }

        private void Transport_Disconnected(object? sender, IDisconnectEventArgs e)
        {
            if (Sessions.TryGetValue(e.SessionId, out var anlysis))
                anlysis.DisconnectTime = DateTime.Now;
        }

        public override async Task OnDisable()
        {
            Helper.Events.Transport.Connecting   -= Transport_Connecting;
            Helper.Events.Transport.Disconnected -= Transport_Disconnected;
            Helper.Events.Transport.Handshaking  -= Transport_Handshaking;
            Helper.Events.Transport.LoginStart   -= Transport_LoginStart;
            Helper.Events.Transport.PacketSendToServer -= Transport_PacketSendToServer;
            Helper.Events.Transport.PacketSendToClient -= Transport_PacketSendToClient;
            if (Sessions.Count > 0)
            {
                string saveDirectory = Path.Combine(Helper.WorkDirectory.FullName, "Data");
                if (!Directory.Exists(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);

                using FileStream fs = new FileStream(Path.Combine(saveDirectory, $"{_startTime : yyyy-MM-dd mmHHssss}.json"), FileMode.OpenOrCreate);
                await JsonSerializer.SerializeAsync(fs, Sessions);
            }
        }
    }
}