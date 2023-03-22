using System;
using Firewall.Rules;
using NyaProxy.API.Command;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets.Client;

namespace Firewall.Commands
{
    internal class RuleCommandParser<T> : Command where T : Rule, new()
    {
        public override string Name { get; }

        public T Rule { get; set; }

        public RuleCommandParser()
        {
            Rule = new T();
            AddOption(new Option("-host", (command, option, helper) => Rule.Host = option.Value));
            AddOption(new Option("-s", (command, option, helper) => Rule.Source = BaseNetworkRuleItem.Parse(option.Value)));
            AddOption(new Option("-sport", (command, option, helper) => (Rule.Source ??= new()).Port = PortRange.Parse(option.Value)));
            AddOption(new Option("-d", (command, option, helper) => Rule.Destination = BaseNetworkRuleItem.Parse(option.Value)));
            AddOption(new Option("-dport", (command, option, helper) => (Rule.Destination ??= new()).Port = PortRange.Parse(option.Value)));
            if (Rule is PacketRule)
            {
                AddOption(new Option("--packet-id", (command, option, helper) => (Rule as PacketRule)!.PacketId = int.Parse(option.Value)));
                AddOption(new Option("--protocol-version", (command, option, helper) => (Rule as HandshakeRule)!.ProtocolVersion = int.Parse(option.Value)));

                if (Rule is LoginRule)
                {
                    AddOption(new Option("--player-name", (command, option, helper) => (Rule as LoginRule)!.PlayerName = option.Value));
                    AddOption(new Option("--player-uuid", (command, option, helper) => (Rule as LoginRule)!.PlayerUUID = UUID.Parse(option.Value)));

                }
                else if (Rule is HandshakeRule)
                {
                    AddOption(new Option("--handshake-address", (command, option, helper) => (Rule as HandshakeRule)!.ServerAddress = option.Value));
                    AddOption(new Option("--handshake-port", (command, option, helper) => (Rule as HandshakeRule)!.ServerPort = ushort.Parse(option.Value)));
                    AddOption(new Option("--handshake-state", (command, option, helper) => (Rule as HandshakeRule)!.NextState = Enum.Parse<HandshakeState>(option.Value)));
                }
            }


            AddOption(new Option("-action", (command, option, helper) => Rule.Action = Enum.Parse<RuleAction>(option.Value)));
            AddOption(new Option("-description", (command, option, helper) => Rule.Description = option.Value));
        }
    }


}
