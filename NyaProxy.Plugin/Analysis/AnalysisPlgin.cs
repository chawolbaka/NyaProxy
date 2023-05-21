using System;
using System.Net;
using NyaProxy.API;
using MinecraftProtocol.Packets.Client;
using Analysis.Commands;

namespace Analysis
{

    public class AnalysisPlgin : NyaPlugin
    {
        public static long ConnectCount { get; private set; }

        public override async Task OnEnable()
        {
            AnalysisData.SetDirectory(Helper.WorkDirectory.FullName);
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
            ServerListPingAnalysisData anlysis = new ServerListPingAnalysisData();
            anlysis.SessionId = e.SessionId;
            anlysis.ConnectTime = DateTime.Now;
            anlysis.Source = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            AnalysisData.Pings.Add(e.SessionId, anlysis);
            ConnectCount++;
        }

        private void Transport_Handshaking(object? sender, IHandshakeEventArgs e)
        {
            if (!AnalysisData.Pings.ContainsKey(e.SessionId))
                return;
            BridgeAnalysisData analysis = AnalysisData.Pings[e.SessionId];
            if(e.Packet.NextState == HandshakeState.Login)
            {
                //如果是登录的握手请求就转换类型
                SessionAnalysisData sessionAnalysis = new SessionAnalysisData();
                sessionAnalysis.SessionId = analysis.SessionId;
                sessionAnalysis.ConnectTime = analysis.ConnectTime;
                sessionAnalysis.Source = analysis.Source;

                analysis = sessionAnalysis;
                AnalysisData.Pings.Remove(e.SessionId);
                AnalysisData.Sessions.Add(e.SessionId, sessionAnalysis);
            }
            analysis.Host = e.Host;
            analysis.HandshakeTime = DateTime.Now;
        }

        private void Transport_LoginStart(object? sender, ILoginStartEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var anlysis))
            {
                anlysis.Player = e.Player;
                anlysis.LoginStartTime = DateTime.Now;
                anlysis.Destination = e.Destination.RemoteEndPoint as IPEndPoint;
            }
        }
        
        private void Transport_LoginSuccess(object? sender, ILoginSuccessEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var anlysis))
                anlysis.LoginSuccessTime = DateTime.Now;
        }

        private void Transport_PacketSendToClient(object? sender, IPacketSendEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var analysis))
                Add(analysis.PacketAnalysis.Server, e);
            else if (AnalysisData.Pings.TryGetValue(e.SessionId, out var pingAnalysis))
                Add(pingAnalysis, e);
        }

        private void Transport_PacketSendToServer(object? sender, IPacketSendEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var analysis))
                Add(analysis.PacketAnalysis.Client, e);
            else if (AnalysisData.Pings.TryGetValue(e.SessionId, out var pingAnalysis))
                Add(pingAnalysis, e);
        }

        private void Add(ServerListPingAnalysisData anslysis, IPacketSendEventArgs e)
        {
            anslysis.BytesTransferred += e.BytesTransferred;
            anslysis.Destination ??= e.Destination.RemoteEndPoint as IPEndPoint;
        }

        private void Add(PacketAnalysisData anslysis, IPacketSendEventArgs e)
        {
            int id = e.Packet.Id;

            if (!anslysis.Table.ContainsKey(id))
                anslysis.Table.Add(id, new TransportAnalysisData());

            anslysis.Table[id].Count++;
            anslysis.Table[id].BytesTransferred += e.BytesTransferred;

            anslysis.Count++;
            anslysis.BytesTransferred += e.BytesTransferred;
        }

        private void Transport_Disconnected(object? sender, IDisconnectEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var anlysis))
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
            await AnalysisData.SaveAsync();
            ConnectCount = 0;
        }
    }
}