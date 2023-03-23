using System;
using Firewall.Rules;
using NyaProxy.API.Command;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets.Client;
using System.Diagnostics.CodeAnalysis;

namespace Firewall.Commands
{
    internal class RuleCommandParser<T> : Command where T : Rule, new()
    {
        public override string Name { get; }

        [AllowNull]
        public T Rule { get; set; }

        public RuleCommandParser()
        {
            AddOption(new Option("-host",  (command, option, helper) => (Rule ??= new T()).Host = option.Value));
            AddOption(new Option("-s",     (command, option, helper) => (Rule ??= new T()).Source = BaseNetworkRuleItem.Parse(option.Value)));
            AddOption(new Option("-sport", (command, option, helper) => ((Rule ??= new T()).Source ??= new()).Port = PortRange.Parse(option.Value)));
            AddOption(new Option("-d",     (command, option, helper) => (Rule ??= new T()).Destination = BaseNetworkRuleItem.Parse(option.Value)));
            AddOption(new Option("-dport", (command, option, helper) => ((Rule ??= new T()).Destination ??= new()).Port = PortRange.Parse(option.Value)));
            if (Rule is PacketRule)
            {
                AddOption(new Option("--packet-id",        (command, option, helper) => ((Rule ??= new T()) as PacketRule)!.PacketId = int.Parse(option.Value)));
                AddOption(new Option("--protocol-version", (command, option, helper) => ((Rule ??= new T()) as HandshakeRule)!.ProtocolVersion = int.Parse(option.Value)));

                if (Rule is LoginRule)
                {
                    AddOption(new Option("--player-name", (command, option, helper) => ((Rule ??= new T()) as LoginRule)!.PlayerName = option.Value));
                    AddOption(new Option("--player-uuid", (command, option, helper) => ((Rule ??= new T()) as LoginRule)!.PlayerUUID = UUID.Parse(option.Value)));

                }
                else if (Rule is HandshakeRule)
                {
                    AddOption(new Option("--handshake-address", (command, option, helper) => ((Rule ??= new T()) as HandshakeRule)!.ServerAddress = option.Value));
                    AddOption(new Option("--handshake-port",    (command, option, helper) => ((Rule ??= new T()) as HandshakeRule)!.ServerPort = ushort.Parse(option.Value)));
                    AddOption(new Option("--handshake-state",   (command, option, helper) => ((Rule ??= new T()) as HandshakeRule)!.NextState = Enum.Parse<HandshakeState>(option.Value)));
                }
            }


            AddOption(new Option("-action", (command, option, helper) => (Rule ??= new T()).Action = Enum.Parse<RuleAction>(option.Value)));
            AddOption(new Option("-description", (command, option, helper) => (Rule ??= new T()).Description = option.Value));
        }
    }


}
