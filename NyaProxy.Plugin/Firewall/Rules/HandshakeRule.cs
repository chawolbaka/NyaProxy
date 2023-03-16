﻿using MinecraftProtocol.Packets.Client;
using System.Xml;

namespace Firewall.Rules
{
    public class HandshakeRule : PacketRule
    {
        public RuleItem<string> ServerAddress { get; set; }

        public RuleItem<ushort> ServerPort { get; set; }

        public RuleItem<HandshakeState> NextState { get; set; }

        public RuleItem<int> ProtocolVersion { get; set; }

        public HandshakeRule() { }

        internal HandshakeRule(XmlReader reader) : base(reader) { }

        protected internal override object Read(XmlReader reader)
        {
            if (base.Read(reader) == null)
            {
                if (reader.Name == nameof(ServerAddress))
                    ServerAddress = new RuleItem<string>(reader, (text) => text);
                else if (reader.Name == nameof(ServerPort))
                    ServerPort = new RuleItem<ushort>(reader, (text) => ushort.Parse(text));
                else if (reader.Name == nameof(NextState))
                    NextState = new RuleItem<HandshakeState>(reader, (text) => Enum.Parse<HandshakeState>(text));
                else if (reader.Name == nameof(ProtocolVersion))
                    ProtocolVersion = new RuleItem<int>(reader, (text) => int.Parse(text));
            }

            return null;
        }

        internal override void Write(XmlWriter writer)
        {
            base.Write(writer);
            ServerAddress?.Write(writer, nameof(ServerAddress));
            ServerPort?.Write(writer, nameof(ServerPort));
            NextState?.Write(writer, nameof(NextState));
            ProtocolVersion?.Write(writer, nameof(ProtocolVersion));
        }
    }
}
