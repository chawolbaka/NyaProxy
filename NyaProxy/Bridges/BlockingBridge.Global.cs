using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using NyaProxy.API.Enum;
using NyaProxy.EventArgs;
using NyaProxy.Channles;
using NyaProxy.Debug;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Pools;

namespace NyaProxy.Bridges
{
    public partial class BlockingBridge : Bridge
    {
        private static BlockingCollection<PacketSendEventArgs>[] ReceiveQueues;
        private static ObjectPool<PacketSendEventArgs> PacketEventArgsPool = new ();
        private static ObjectPool<ChatSendEventArgs> ChatEventArgsPool = new();
        private static ObjectPool<PluginChannleSendEventArgs> PluginChannleEventArgsPool = new();
        private static SafeIndex QueueIndex;


        internal static void Setup(int networkThread) 
        {
            if (ReceiveQueues != null)
            {
                foreach (var queue in ReceiveQueues)
                {
                    queue.Dispose();
                }
                ReceiveQueues = null;
            }

            QueueIndex = new SafeIndex(networkThread);
            ReceiveQueues = new BlockingCollection<PacketSendEventArgs>[networkThread];
            for (int i = 0; i < ReceiveQueues.Length; i++)
                ReceiveQueues[i] = new BlockingCollection<PacketSendEventArgs>();


            for (int i = 0; i < networkThread; i++)
            {
                var queue = ReceiveQueues[i];
                Thread thread = new Thread(() => {
                    AppDomain.CurrentDomain.UnhandledException += (sender, e) => Crash.Report(e.ExceptionObject as Exception);
                    QueueHandler(queue, ReceiveQueueHandler);
                });
                thread.IsBackground = true;
                thread.Name = $"Network IO #{i + 1}";
                thread.Start();
            }
        }

      


        private static void ReceiveQueueHandler(PacketSendEventArgs e)
        {
            try
            {
                if (e.Bridge.Source == null || e.Bridge.Destination == null || !e.Bridge.Source.Connected || !e.Bridge.Destination.Connected)
                    return;
#if DEBUG
                if (true)
#else
                if (NyaProxy.Plugin.Count > 0)
#endif
                {
                    //触发事件
                    try
                    {
                        if (e is ChatSendEventArgs csea)
                            (e.Direction == Direction.ToClient ? NyaProxy.ChatMessageSendToClient : NyaProxy.ChatMessageSendToServer).Invoke(e.Bridge, csea);
                        else
                            (e.Direction == Direction.ToClient ? NyaProxy.PacketSendToClient : NyaProxy.PacketSendToServer).Invoke(e.Bridge, e);
                        
                        if (!e.IsBlock && e is PluginChannleSendEventArgs pcsea && NyaProxy.Channles.ContainsKey(pcsea.ChannleName))
                        {
                            Channle channle = NyaProxy.Channles[pcsea.ChannleName] as Channle;
                            channle?.Trigger(e.Direction == Direction.ToServer ? Side.Client : Side.Server, new ByteReader(pcsea.Data), e.Bridge);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is SocketException)
                            e.Bridge?.Break();
                        NyaProxy.Logger.Exception(ex);
                    }
                }

                if (!e.IsBlock)
                {
                    //如果数据没有被修改过那么就直接发送接收到的原始数据，避免Pack造成的内存分配。
                    if (e.EventArgs != null && !e.Bridge.IsOnlineMode && !e.Bridge.OverCompression && !e.PacketCheaged)
                    {
                        var rawData = e.EventArgs.RawData.Span;
                        for (int i = 0; i < rawData.Length; i++)
                        {
                            if (rawData[i].Length > 0)
                                NyaProxy.Network.Enqueue(e.Destination, rawData[i], i + 1 < e.EventArgs.RawData.Length ? null : e.EventArgs);
                        }
                    }
                    else
                    {
                        if (e.Direction == Direction.ToClient)
                            NyaProxy.Network.Enqueue(e.Destination, e.Bridge.CryptoHandler.TryEncrypt(e.Packet.Pack()), (IDisposable)e.EventArgs ?? e.Packet);
                        else
                            NyaProxy.Network.Enqueue(e.Destination, e.Packet.Pack(), (IDisposable)e.EventArgs ?? e.Packet);
                    }
                }
                else
                {
                    if (e.EventArgs == null)
                        e.EventArgs?.Dispose();
                    else
                        e.Packet?.Dispose();
                }
            }
            catch (Exception ex)
            {
                e.Bridge.Break();
                NyaProxy.Logger.Exception(ex);
            }
            finally
            {
                if (e is ChatSendEventArgs)
                    ChatEventArgsPool.Return(e as ChatSendEventArgs);
                else if (e is PluginChannleSendEventArgs)
                    PluginChannleEventArgsPool.Return(e as PluginChannleSendEventArgs);
                else
                    PacketEventArgsPool.Return(e);   
            }
        }

        private static void QueueHandler<T>(BlockingCollection<T> queue, Action<T> action)
        {
            try
            {
                while (!NyaProxy.GlobalQueueToken.IsCancellationRequested)
                {
                    action(queue.Take(NyaProxy.GlobalQueueToken.Token));
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                NyaProxy.GlobalQueueToken.Cancel();
                NyaProxy.Logger.Exception(e);
            }
        }
    }
}
