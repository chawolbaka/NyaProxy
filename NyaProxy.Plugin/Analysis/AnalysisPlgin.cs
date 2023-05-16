using System;
using System.Net;
using System.Text.Json;
using NyaProxy.API;
using MinecraftProtocol.Packets.Client;

namespace Analysis
{

    public class AnalysisPlgin : NyaPlugin
    {
        public static Dictionary<long, PingAnalysis> Pings       { get; private set; }
        public static Dictionary<long, SessionAnalysis> Sessions { get; private set; }
        public static long ConnectCount { get; private set; }
        private DateTime _startTime;

        public override async Task OnEnable()
        {
            _startTime = DateTime.Now;
            Pings = new();
            Sessions = new();

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
            PingAnalysis anlysis = new PingAnalysis();
            anlysis.SessionId = e.SessionId;
            anlysis.ConnectTime = DateTime.Now;
            anlysis.Source = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            Pings.Add(e.SessionId, anlysis);
            ConnectCount++;
        }

        private void Transport_Handshaking(object? sender, IHandshakeEventArgs e)
        {
            if (!Pings.ContainsKey(e.SessionId))
                return;
            BridgeAnalysis analysis = Pings[e.SessionId];
            if(e.Packet.NextState == HandshakeState.Login)
            {
                //如果是登录的握手请求就转换类型
                SessionAnalysis sessionAnalysis = new SessionAnalysis();
                sessionAnalysis.SessionId = analysis.SessionId;
                sessionAnalysis.ConnectTime = analysis.ConnectTime;
                sessionAnalysis.Source = analysis.Source;

                analysis = sessionAnalysis;
                Pings.Remove(e.SessionId);
                Sessions.Add(e.SessionId, sessionAnalysis);
            }
            analysis.Host = e.Host;
            analysis.HandshakeTime = DateTime.Now;
        }

        private void Transport_LoginStart(object? sender, ILoginStartEventArgs e)
        {
            if (Sessions.TryGetValue(e.SessionId, out var anlysis))
            {
                anlysis.Player = e.Player;
                anlysis.LoginStartTime = DateTime.Now;
                anlysis.Destination = e.Destination.RemoteEndPoint as IPEndPoint;
            }
        }
        
        private void Transport_LoginSuccess(object? sender, ILoginSuccessEventArgs e)
        {
            if (Sessions.TryGetValue(e.SessionId, out var anlysis))
                anlysis.LoginSuccessTime = DateTime.Now;
        }

        private void Transport_PacketSendToClient(object? sender, IPacketSendEventArgs e)
        {
            if (Sessions.TryGetValue(e.SessionId, out var analysis))
                Add(analysis.PacketAnalysis.Server, e);
            else if (Pings.TryGetValue(e.SessionId, out var pingAnalysis))
                Add(pingAnalysis, e);
        }

        private void Transport_PacketSendToServer(object? sender, IPacketSendEventArgs e)
        {
            if (Sessions.TryGetValue(e.SessionId, out var analysis))
                Add(analysis.PacketAnalysis.Client, e);
            else if (Pings.TryGetValue(e.SessionId, out var pingAnalysis))
                Add(pingAnalysis, e);
        }

        private void Add(PingAnalysis anslysis, IPacketSendEventArgs e)
        {
            anslysis.BytesTransferred += e.BytesTransferred;
            anslysis.Destination ??= e.Destination.RemoteEndPoint as IPEndPoint;
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
            if (Pings.Count > 0 || Sessions.Count > 0)
            {
                string saveDirectory = Path.Combine(Helper.WorkDirectory.FullName, "Data");
                if (!Directory.Exists(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);

                if (Pings.Count > 0)
                {
                    using FileStream fs = new FileStream(Path.Combine(saveDirectory, $"ping-{_startTime: yyyy-MM-dd mmHHssss}.json"), FileMode.OpenOrCreate);
                    await JsonSerializer.SerializeAsync(fs, Sessions);
                }
                if (Sessions.Count > 0)
                {
                    using FileStream fs = new FileStream(Path.Combine(saveDirectory, $"play-{_startTime: yyyy-MM-dd mmHHssss}.json"), FileMode.OpenOrCreate);
                    await JsonSerializer.SerializeAsync(fs, Sessions);
                }
            }
            Pings = null;
            Sessions = null;
            ConnectCount = 0;
        }
    }
}