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

        public RuleCommandParser(string name)
        {
            Name = name;
            AddOption(new Option("-h", 1, (command, e) => (Rule ??= new T()).Host = e.Arguments.Span[0], "--host"));
            AddOption(new Option("-s", 1, (command, e) => (Rule ??= new T()).Source = BaseNetworkRuleItem.Parse(e.Arguments.Span[0]), "--source"));
            AddOption(new Option("-d", 1, (command, e) => (Rule ??= new T()).Destination = BaseNetworkRuleItem.Parse(e.Arguments.Span[0]), "--destination"));
            AddOption(new Option("-sport", 1, (command, e) => ((Rule ??= new T()).Source ??= new()).Port = PortRange.Parse(e.Arguments.Span[0]), "--source-port"));
            AddOption(new Option("-dport", 1, (command, e) => ((Rule ??= new T()).Destination ??= new()).Port = PortRange.Parse(e.Arguments.Span[0]), "--destination-port"));
            if (Rule is PacketRule)
            {
                AddOption(new Option("--packet-id", 1, (command, e) => ((Rule ??= new T()) as PacketRule)!.PacketId = int.Parse(e.Arguments.Span[0])));
                AddOption(new Option("--protocol-version", 1, (command, e) => ((Rule ??= new T()) as HandshakeRule)!.ProtocolVersion = int.Parse(e.Arguments.Span[0])));

                if (Rule is LoginRule)
                {
                    AddOption(new Option("-name", 1, (command, e) => ((Rule ??= new T()) as LoginRule)!.PlayerName = e.Arguments.Span[0], "--player-name"));
                    AddOption(new Option("-uuid", 1, (command, e) => ((Rule ??= new T()) as LoginRule)!.PlayerUUID = UUID.Parse(e.Arguments.Span[0]), "--player-uuid"));

                }
                else if (Rule is HandshakeRule)
                {
                    AddOption(new Option("--handshake-address", 1, (command, e) => ((Rule ??= new T()) as HandshakeRule)!.ServerAddress = e.Arguments.Span[0]));
                    AddOption(new Option("--handshake-port",  1, (command, e) => ((Rule ??= new T()) as HandshakeRule)!.ServerPort = ushort.Parse(e.Arguments.Span[0])));
                    AddOption(new Option("--handshake-state", 1, (command, e) => ((Rule ??= new T()) as HandshakeRule)!.NextState = Enum.Parse<HandshakeState>(e.Arguments.Span[0])));
                }
            }


            AddOption(new Option("-a", 1, (command, e) => (Rule ??= new T()).Action = Enum.Parse<RuleAction>(e.Arguments.Span[0]), "--action"));
            AddOption(new Option("--description", 1, (command, e) => (Rule ??= new T()).Description = e.Arguments.Span[0]));
        }
    }


}
