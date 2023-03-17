using System.Collections.Generic;
using MinecraftProtocol.Packets.Server;

namespace NyaProxy.Extension
{
    public static class PacketCache
    {
        public static Dictionary<string, byte[]> Disconnect = new Dictionary<string, byte[]>();
        public static Dictionary<string, byte[]> DisconnectLogin = new Dictionary<string, byte[]>();

        public static byte[] Get(this Dictionary<string, byte[]> cache, string message)
        {
            if (!cache.ContainsKey(message))
                cache.Add(message, new DisconnectLoginPacket(message, -1).Pack(-1));
         
            return cache[message];
        }
    }
}
