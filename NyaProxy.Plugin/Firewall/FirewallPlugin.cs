using System.Net;
using System.Net.Sockets;
using NyaProxy.API;
using Firewall.Rules;
using Firewall.Tables;
using MinecraftProtocol.DataType;

namespace Firewall
{

    public class FirewallPlugin : NyaPlugin
    {
        public override async Task OnEnable()
        {
            await Firewall.Chains.LoadAsync(Helper.WorkDirectory.FullName);

            Helper.Events.Transport.Connecting  += OnConnecting;
            Helper.Events.Transport.Handshaking += OnHandshaking;
            Helper.Events.Transport.LoginStart  += OnLoginStart;
            Helper.Events.Transport.PacketSendToClient += OnPacketSendToClient;
            Helper.Events.Transport.PacketSendToServer += OnPacketSendToServer;
        }

        public override async Task OnDisable()
        {
            Helper.Events.Transport.Connecting  -= OnConnecting;
            Helper.Events.Transport.Handshaking -= OnHandshaking;
            Helper.Events.Transport.LoginStart  -= OnLoginStart;
            Helper.Events.Transport.PacketSendToClient -= OnPacketSendToClient;
            Helper.Events.Transport.PacketSendToServer -= OnPacketSendToServer;
            await Firewall.Chains.SaveAsync(Helper.WorkDirectory.FullName);
        }


        private void OnConnecting(object? sender, IConnectEventArgs e)
        {
            IPEndPoint source = (IPEndPoint)e.AcceptSocket.RemoteEndPoint!;

            for (int i = 0; i < Firewall.Chains.Connect.FilterTable.Rules.Count; i++)
            {
                Rule rule = Firewall.Chains.Connect.FilterTable.Rules[i];
                if (rule.Disabled)
                    continue;

                if (rule.Source != null && !rule.Source.Match(source.Address, source.Port))
                    continue;

                if (ExecuteAction(rule, e))
                    return;
            }

            //SYN > SYN ACK > RST ≈ 端口扫描
            if (e.SocketError == SocketError.ConnectionReset)
            {
                Firewall.Chains.Connect.FilterTable.Rules.Add(new Rule() { Source = source.Address, Action = RuleAction.Block });
                e.Block();
            }
        }

        private void OnHandshaking(object? sender, IHandshakeEventArgs e)
        {
            IPEndPoint source = (IPEndPoint)e.Source.RemoteEndPoint!;
            string host = e.Packet.GetServerAddressOnly();

            for (int i = 0; i < Firewall.Chains.Handshake.FilterTable.Rules.Count; i++)
            {
                HandshakeRule rule = Firewall.Chains.Handshake.FilterTable.Rules[i];
                if (rule.Disabled)
                    continue;

                if (rule.Source != null && !rule.Source.Match(source.Address, source.Port))
                    continue;

                if (rule.Host != null && rule.Host.Match(host))
                    continue;
                if (rule.ServerAddress != null && rule.ServerAddress.Match(e.Packet.ServerAddress))
                    continue;
                if (rule.ServerPort != null && rule.ServerPort.Match(e.Packet.ServerPort))
                    continue;
                if (rule.ProtocolVersion != null && rule.ProtocolVersion.Match(e.Packet.ProtocolVersion))
                    continue;
                if (rule.NextState != null && rule.NextState.Match(e.Packet.NextState))
                    continue;

                if (ExecuteAction(rule, e))
                    return;
            }
        }

        private void OnLoginStart(object? sender, ILoginStartEventArgs e)
        {
            for (int i = 0; i < Firewall.Chains.Login.FilterTable.Rules.Count; i++)
            {
                LoginRule rule = Firewall.Chains.Login.FilterTable.Rules[i];
                if (HasNotMatchPacketRule(rule, e))
                    continue;
                if (rule.PlayerName != null && rule.PlayerName.Match(e.PlayerName))
                    continue;
                if (rule.PlayerUUID != null && rule.PlayerUUID.Match(e.PlayerUUID != UUID.Empty ? e.PlayerUUID : UUID.GetFromPlayerName(e.PlayerName)))
                    continue;

                if (ExecuteAction(rule, e))
                    return;
            }
        }

        private void OnPacketSendToServer(object? sender, IPacketSendEventArgs e)
        {
            PacketFilter(Firewall.Chains.Input.FilterTable, e);
        }

        private void OnPacketSendToClient(object? sender, IPacketSendEventArgs e)
        {
            PacketFilter(Firewall.Chains.Output.FilterTable, e);
        }

        private void PacketFilter(Table<PacketRule> table, IPacketSendEventArgs e)
        {
            for (int i = 0; i < table.Rules.Count; i++)
            {
                PacketRule rule = table.Rules[i];
                if (HasNotMatchPacketRule(rule, e))
                    continue;

                if (ExecuteAction(rule, e))
                    return;
            }
        }

        private bool ExecuteAction(Rule rule, IBlockEventArgs e)
        {
            switch (rule.Action)
            {
                case RuleAction.Pass: return true;
                case RuleAction.Block: e.Block(); return false;
                default: throw new InvalidOperationException();
            }
        }

        private bool HasNotMatchPacketRule(PacketRule rule, IPacketSendEventArgs e)
        {
            IPEndPoint source = (IPEndPoint)e.Source.RemoteEndPoint!;
            IPEndPoint destination = (IPEndPoint)e.Destination.RemoteEndPoint!;

            if (rule.Disabled)
                return false;

            if (rule.Host != null && !rule.Host.Match(e.Host))
                return true;

            if (rule.PacketId != null && !rule.PacketId.Match(e.Packet.Id))
                return true;

            if (rule.Source != null && !rule.Source.Match(source.Address,source.Port))
                return true;

            if (rule.Destination != null && !rule.Destination.Match(destination.Address, destination.Port))
                return true;
            
            return false;
        }

    }
}