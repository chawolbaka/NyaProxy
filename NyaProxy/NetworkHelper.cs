using System;
using System.Net.Sockets;
using NyaProxy.API;
using NyaProxy.Bridges;
using MinecraftProtocol.Packets;
using MinecraftProtocol.IO;
using System.Threading;
using NyaProxy.Debug;

namespace NyaProxy
{
    public class NetworkHelper : INetworkHelper
    {
        private NetworkSender sender = new NetworkSender();

        internal NetworkHelper(CancellationToken token)
        {
            Thread sendThread = new Thread(() => {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => Crash.Report(e.ExceptionObject as Exception);
                sender.Start(token);
            });
            sendThread.IsBackground = true;
            sendThread.Name = "Network IO #0";
            sendThread.Start();
        }

        public void Enqueue(Socket socket, ICompatiblePacket packet)
        {
            sender.Enqueue(socket, packet.Pack());
        }

        public void Enqueue(Socket socket, Memory<byte> data)
        {
            sender.Enqueue(socket, data);
        }

        public void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable)
        {
            sender.Enqueue(socket, data, disposable);
        }

        public void Enqueue(Socket socket, Memory<byte> data, Action callback)
        {
            sender.Enqueue(socket, data, callback);
        }

    }
}
