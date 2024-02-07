using System.Net;
using System.Net.Sockets;
using NyaProxy.API;
using NyaProxy.API.Command;
using NyaFirewall.Rules;
using NyaFirewall.Tables;
using MinecraftProtocol.DataType;
using NyaProxy.API.Event;
using Microsoft.Extensions.Logging;

namespace NyaFirewall
{

    public class FirewallPlugin : NyaPlugin
    {
        public static FirewallConfig Config => CurrentInstance.Helper.Config.Get<FirewallConfig>(0);
        public static FirewallPlugin CurrentInstance;

        public override async Task OnEnable()
        {
            CurrentInstance = this;
            Helper.Config.Register(typeof(FirewallConfig));

            await Firewall.Chains.LoadAsync(Helper.WorkDirectory.FullName);
            Logger.LogInformation($"{Firewall.Chains.Count()} rules loaded.");
            Helper.Events.Transport.Connecting  += OnConnecting;
            Helper.Events.Transport.Handshaking += OnHandshaking;
            Helper.Events.Transport.LoginStart  += OnLoginStart;
            Helper.Events.Transport.PacketSendToClient += OnPacketSendToClient;
            Helper.Events.Transport.PacketSendToServer += OnPacketSendToServer;
            Helper.Command.Register(new SimpleCommand("print", async (args, helper) => helper.Logger.LogInformation($"{Firewall.Chains.Count()} rules loaded.", Firewall.Chains.ToStringTable())));
            Firewall.Chains.RegisterCommand(Helper.Command);
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

        [EventPriority(EventPriority.Highest)]
        private void OnConnecting(object? sender, IConnectEventArgs e)
        {
            IPEndPoint source = (IPEndPoint)e.AcceptSocket.RemoteEndPoint!;
            //SYN > SYN ACK > RST ≈ 端口扫描
            if (e.SocketError == SocketError.ConnectionReset)
                Firewall.Chains.Connect.FilterTable.Rules.AddFirst(new Rule() { Source = source.Address, Action = RuleAction.Block });

            foreach (var rule in Firewall.Chains.Connect.FilterTable)
            {
                if (rule.Source != null && !rule.Source.Match(source.Address, source.Port))
                    continue;

                switch (rule.Action)
                {
                    case RuleAction.Pass:   return;
                    case RuleAction.Block:  e.Block(); return;
                    case RuleAction.Reject: e.Block(); e.AcceptSocket.Close(); return;
                    default: throw new InvalidOperationException();
                }
            }
        }

        [EventPriority(EventPriority.Highest)]
        private void OnHandshaking(object? sender, IHandshakeEventArgs e)
        {
            IPEndPoint source = (IPEndPoint)e.Source.RemoteEndPoint!;
            string host = e.Packet.GetServerAddressOnly();

            foreach (var rule in Firewall.Chains.Handshake.FilterTable)
            {

                if (rule.Source != null && !rule.Source.Match(source.Address, source.Port))
                    continue;

                if (rule.Host != null && !rule.Host.Match(host))
                    continue;
                if (rule.PacketId != null && !rule.PacketId.Match(e.Packet.Id))
                    continue;
                if (rule.ServerAddress != null && !rule.ServerAddress.Match(e.Packet.ServerAddress))
                    continue;
                if (rule.ServerPort != null && !rule.ServerPort.Match(e.Packet.ServerPort))
                    continue;
                if (rule.ProtocolVersion != null && !rule.ProtocolVersion.Match(e.Packet.ProtocolVersion))
                    continue;
                if (rule.NextState != null && !rule.NextState.Match(e.Packet.NextState))
                    continue;

                switch (rule.Action)
                {
                    case RuleAction.Pass:   return;
                    case RuleAction.Block:  e.Block(); return;
                    case RuleAction.Reject: e.Block(); e.Source.Close(); return;
                    default: throw new InvalidOperationException();
                }
            }
        }

        [EventPriority(EventPriority.Highest)]
        private void OnLoginStart(object? sender, ILoginStartEventArgs e)
        {
            foreach (var rule in Firewall.Chains.Login.FilterTable)
            {
                if (HasNotMatchPacketRule(rule, e))
                    continue;

                if (rule.PlayerName != null && !rule.PlayerName.Match(e.PlayerName))
                    continue;
                if (rule.PlayerUUID != null && !rule.PlayerUUID.Match(e.PlayerUUID != UUID.Empty ? e.PlayerUUID : UUID.GetFromPlayerName(e.PlayerName)))
                    continue;
               
                switch (rule.Action)
                {
                    case RuleAction.Pass: return;
                    case RuleAction.Block:  e.Block(); return;
                    case RuleAction.Reject: e.Block(); e.Player.KickAsync("你已被防火墙拦截。").Wait(); return;
                    default: throw new InvalidOperationException();
                }
            }
        }

        [EventPriority(EventPriority.Highest)]
        private void OnPacketSendToServer(object? sender, IPacketSendEventArgs e)
        {
            PacketFilter(Firewall.Chains.Client.FilterTable, e);
        }

        [EventPriority(EventPriority.Highest)]
        private void OnPacketSendToClient(object? sender, IPacketSendEventArgs e)
        {
            PacketFilter(Firewall.Chains.Server.FilterTable, e);
        }

        private void PacketFilter(Table<PacketRule> table, IPacketSendEventArgs e)
        {
            foreach (var rule in table)
            {
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

            if (rule.Host != null && !rule.Host.Match(e.Host))
                return true;

            if (rule.PacketId != null && !rule.PacketId.Match(e.Packet.Id))
                return true;

            if (rule.ProtocolVersion != null && !rule.ProtocolVersion.Match(e.ProtocolVersion))
                return true;

            if (rule.Source != null && !rule.Source.Match(source.Address,source.Port))
                return true;

            if (rule.Destination != null && !rule.Destination.Match(destination.Address, destination.Port))
                return true;
            
            return false;
        }

    }
}