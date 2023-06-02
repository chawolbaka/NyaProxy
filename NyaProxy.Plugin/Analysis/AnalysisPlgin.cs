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

        public static readonly int StartIndex = 1001;

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
            if (e.SessionId > AnalysisData.Pings.Capacity)
                AnalysisData.Pings.Capacity = (int)e.SessionId * 2;
      

            ServerListPingRecord record = new ServerListPingRecord();
            record.SessionId = e.SessionId;
            record.ConnectTime = DateTime.Now;
            record.Source = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            AnalysisData.Sessions.Add(null);
            AnalysisData.Pings.Add(null);
            AnalysisData.Pings[(int)e.SessionId - StartIndex] = record;
            ConnectCount++;
        }

        private void Transport_Handshaking(object? sender, IHandshakeEventArgs e)
        {
            int index = (int)e.SessionId - StartIndex;
            if (AnalysisData.Pings[index] == null)
                return;

            BridgeRecord record = AnalysisData.Pings[index];
            if(e.Packet.NextState == HandshakeState.Login)
            {
                if (e.SessionId > AnalysisData.Sessions.Capacity)
                    AnalysisData.Sessions.Capacity = (int)e.SessionId * 2;
                

                //如果是登录的握手请求就转换类型
                SessionRecord sessionRecord = new SessionRecord();
                sessionRecord.SessionId = record.SessionId;
                sessionRecord.ConnectTime = record.ConnectTime;
                sessionRecord.Source = record.Source;
                sessionRecord.ProtocolVersion = e.Packet.ProtocolVersion;
                record = sessionRecord;
                AnalysisData.Pings[index] = null;
                AnalysisData.Sessions[index] = sessionRecord;
            }
            record.Host = e.Host;
            record.HandshakeTime = DateTime.Now;
        }

        private void Transport_LoginStart(object? sender, ILoginStartEventArgs e)
        {
            int index = (int)e.SessionId - StartIndex;
            if (AnalysisData.Sessions[index] != null)
            {
                var record = AnalysisData.Sessions[index];
                record.Player = e.Player;
                record.LoginStartTime = e.ReceivedTime;
                record.Destination = e.Destination.RemoteEndPoint as IPEndPoint;
            }
        }
        
        private void Transport_LoginSuccess(object? sender, ILoginSuccessEventArgs e)
        {
            int index = (int)e.SessionId - StartIndex;
            if (AnalysisData.Sessions[index] != null)
            {
                var record = AnalysisData.Sessions[index];
                record.CompressionThreshold = e.CompressionThreshold;
                record.LoginSuccessTime = e.ReceivedTime;
            }   
        }

        private void Transport_PacketSendToClient(object? sender, IPacketSendEventArgs e)
        {
            int index = (int)e.SessionId - StartIndex;
            if (AnalysisData.Sessions[index] != null)
                Add(AnalysisData.Sessions[index].PacketAnalysis.Server, e);
            else if (AnalysisData.Pings[index] != null)
                Add(AnalysisData.Pings[index], e);
            

        }

        private void Transport_PacketSendToServer(object? sender, IPacketSendEventArgs e)
        {
            int index = (int)e.SessionId - StartIndex;
            if (AnalysisData.Sessions[index] != null)
                Add(AnalysisData.Sessions[index].PacketAnalysis.Client, e);
            else if (AnalysisData.Pings[index] != null)
                Add(AnalysisData.Pings[index], e);
        }

        private void Add(ServerListPingRecord record, IPacketSendEventArgs e)
        {
            record.BytesTransferred += e.BytesTransferred;
            record.Destination ??= e.Destination.RemoteEndPoint as IPEndPoint;
        }

        private void Add(PacketRecord record, IPacketSendEventArgs e)
        {
            int id = e.Packet.Id;

            if (!record.Table.ContainsKey(id))
                record.Table.Add(id, new TransportRecord());

            record.Table[id].Count++;
            record.Table[id].BytesTransferred += e.BytesTransferred;

            record.Count++;
            record.BytesTransferred += e.BytesTransferred;
        }

        private void Transport_Disconnected(object? sender, IDisconnectEventArgs e)
        {
            int index = (int)e.SessionId - StartIndex;
            if (AnalysisData.Sessions.Count < index && AnalysisData.Sessions[index] != null)
            {
                var record = AnalysisData.Sessions[index];
                record.DisconnectTime = DateTime.Now;
            }
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