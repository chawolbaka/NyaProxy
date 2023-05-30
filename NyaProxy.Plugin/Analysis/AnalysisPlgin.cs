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

        public static AnalysisConfig Config => CurrentInstance.Helper.Config.Get<AnalysisConfig>(0);
        public static AnalysisPlgin CurrentInstance;

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
            Helper.Config.Register(typeof(AnalysisConfig));
            Helper.Command.Register(new OverviewCommand());
            CurrentInstance = this;
        }


        private void Transport_Connecting(object? sender, IConnectEventArgs e)
        {
            ServerListPingAnalysisData record = new ServerListPingAnalysisData();
            record.SessionId = e.SessionId;
            record.ConnectTime = DateTime.Now;
            record.Source = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            AnalysisData.Pings.Add(e.SessionId, record);
            ConnectCount++;
        }

        private void Transport_Handshaking(object? sender, IHandshakeEventArgs e)
        {
            if (!AnalysisData.Pings.ContainsKey(e.SessionId))
                return;
            BridgeAnalysisData record = AnalysisData.Pings[e.SessionId];
            if(e.Packet.NextState == HandshakeState.Login)
            {
                //如果是登录的握手请求就转换类型
                SessionAnalysisData sessionRecord = new SessionAnalysisData();
                sessionRecord.SessionId = record.SessionId;
                sessionRecord.ConnectTime = record.ConnectTime;
                sessionRecord.Source = record.Source;
                sessionRecord.ProtocolVersion = e.Packet.ProtocolVersion;

                record = sessionRecord;
                AnalysisData.Pings.Remove(e.SessionId);
                AnalysisData.Sessions.Add(e.SessionId, sessionRecord);
            }
            record.Host = e.Host;
            record.HandshakeTime = DateTime.Now;
        }

        private void Transport_LoginStart(object? sender, ILoginStartEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var record))
            {
                record.Player = e.Player;
                record.LoginStartTime = e.ReceivedTime;
                record.Destination = e.Destination.RemoteEndPoint as IPEndPoint;
            }
        }
        
        private void Transport_LoginSuccess(object? sender, ILoginSuccessEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var record))
            {
                record.CompressionThreshold = e.CompressionThreshold;
                record.LoginSuccessTime = e.ReceivedTime;
            }   
        }

        private void Transport_PacketSendToClient(object? sender, IPacketSendEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var sessionRecord))
                Add(sessionRecord.PacketAnalysis.Server, e);
            else if (AnalysisData.Pings.TryGetValue(e.SessionId, out var pingRecord))
                Add(pingRecord, e);
        }

        private void Transport_PacketSendToServer(object? sender, IPacketSendEventArgs e)
        {
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var sessionRecord))
                Add(sessionRecord.PacketAnalysis.Client, e);
            else if (AnalysisData.Pings.TryGetValue(e.SessionId, out var pingRecord))
                Add(pingRecord, e);
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
            if (AnalysisData.Sessions.TryGetValue(e.SessionId, out var record))
                record.DisconnectTime = DateTime.Now;
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