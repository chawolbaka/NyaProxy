using System;
using System.Net.Sockets;
using NyaProxy.API;
using NyaProxy.Bridges;
using MinecraftProtocol.Packets;

namespace NyaProxy.Plugin
{
    internal partial class PluginHelper
    {
        private class NetworkHelper : INetworkHelper
        {
            public void Enqueue(Socket socket, ICompatiblePacket packet)
            {
                BlockingBridge.Enqueue(socket, packet.Pack());
            }

            public void Enqueue(Socket socket, Memory<byte> data)
            {
                BlockingBridge.Enqueue(socket, data);
            }

            public void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable)
            {
                BlockingBridge.Enqueue(socket, data, disposable);
            }

        }

    }
}
