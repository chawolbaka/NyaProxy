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
using System.Collections.Generic;
using MinecraftProtocol.Compression;
using System.Buffers;
using System.Linq;
using System.Reflection;

namespace NyaProxy.Bridges
{
    public partial class BlockingBridge : Bridge
    {
        private static BlockingCollection<PacketSendEventArgs>[] ReceiveBlockingQueues;
        private static ConcurrentQueue<PacketSendEventArgs>[] ReceiveQueues;
        private static ObjectPool<PacketSendEventArgs> PacketEventArgsPool = new ();
        private static ObjectPool<ChatSendEventArgs> ChatEventArgsPool = new();
        private static ObjectPool<PluginChannleSendEventArgs> PluginChannleEventArgsPool = new();
        private static SafeIndex QueueIndex;
        private static bool EnableBlockingQueue;
            
        internal static void Setup(int networkThread) 
        {
            EnableBlockingQueue = NyaProxy.Config.EnableBlockingQueue; //热重载线程不安全，虽然能处理但我懒的处理qwq
            QueueIndex = new SafeIndex(networkThread);
            ReceiveBlockingQueues = new BlockingCollection<PacketSendEventArgs>[networkThread];
            ReceiveQueues = new ConcurrentQueue<PacketSendEventArgs>[networkThread];
            if (EnableBlockingQueue)
            {
                for (int i = 0; i < ReceiveBlockingQueues.Length; i++)
                    ReceiveBlockingQueues[i] = new();
            }
            else
            {
                for (int i = 0; i < ReceiveQueues.Length; i++)
                    ReceiveQueues[i] = new();
            }


            for (int i = 0; i < networkThread; i++)
            {
                Thread thread = new Thread(ReceiveQueueHandler);
                thread.IsBackground = true;
                thread.Name = $"Network IO #{i + 1}";
                thread.Start(i);
            }
        }

        private static void ReceiveQueueHandler(object index)
        {
            var queue = ReceiveQueues[(int)index];
            var blockingQueue = ReceiveBlockingQueues[(int)index];
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => Crash.Report(e.ExceptionObject as Exception);
            try
            {
                PacketSendEventArgs psea;
                while (!NyaProxy.GlobalQueueToken.IsCancellationRequested)
                {
                    if (EnableBlockingQueue)
                    {
                        psea = blockingQueue.Take(NyaProxy.GlobalQueueToken.Token);
                        if (psea == null)
                            continue;
                    }
                    else
                    {
                        while (!queue.TryDequeue(out psea) || psea == null)
                        {
                            Thread.Sleep(200 / ((int)Bridge.Count + 1));
                        }
                    }

                    try
                    {
#if DEBUG
                        if (true)
#else
                if (NyaProxy.Plugin.Count > 0)
#endif
                        {
                            //触发事件
                            try
                            {
                                if (psea is ChatSendEventArgs csea)
                                    (psea.Direction == Direction.ToClient ? NyaProxy.ChatMessageSendToClient : NyaProxy.ChatMessageSendToServer).Invoke(psea.Bridge, csea);
                                else
                                    (psea.Direction == Direction.ToClient ? NyaProxy.PacketSendToClient : NyaProxy.PacketSendToServer).Invoke(psea.Bridge, psea);

                                if (!psea.IsBlock && psea is PluginChannleSendEventArgs pcsea && NyaProxy.Channles.ContainsKey(pcsea.ChannleName))
                                {
                                    Channle channle = NyaProxy.Channles[pcsea.ChannleName] as Channle;
                                    channle?.Trigger(psea.Direction == Direction.ToServer ? Side.Client : Side.Server, new ByteReader(pcsea.Data), psea.Bridge);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex is SocketException)
                                    psea.Bridge?.Break();
                                NyaProxy.Logger.Exception(ex);
                            }
                        }

                        if (!psea.IsBlock && psea.Bridge.Destination != null && psea.Bridge.Destination.Connected)
                        {
                            //如果数据没有被修改过那么就直接发送接收到的原始数据，避免Pack造成的内存分配。
                            if (psea.EventArgs != null && !psea.Bridge.IsOnlineMode && !psea.Bridge.OverCompression && !psea.PacketCheaged)
                            {
                                var rawData = psea.EventArgs.RawData.Span;
                                for (int i = 0; i < rawData.Length; i++)
                                {
                                    if (rawData[i].Length > 0)
                                        NyaProxy.Network.Enqueue(psea.Destination, rawData[i], i + 1 < psea.EventArgs.RawData.Length ? null : psea.EventArgs);
                                }
                            }
                            else
                            {
                                if (psea.Direction == Direction.ToClient)
                                    NyaProxy.Network.Enqueue(psea.Destination, psea.Bridge.CryptoHandler.TryEncrypt(psea.Packet.Pack()), (IDisposable)psea.EventArgs ?? psea.Packet);
                                else
                                    NyaProxy.Network.Enqueue(psea.Destination, psea.Packet.Pack(), (IDisposable)psea.EventArgs ?? psea.Packet);
                            }
                        }
                        else
                        {
                            if (psea.EventArgs != null)
                                psea.EventArgs.Dispose();
                            else
                                psea.Packet?.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        psea.Bridge.Break();
                        NyaProxy.Logger.Exception(ex);
                    }
                    finally
                    {
                        if (psea is ChatSendEventArgs)
                            ChatEventArgsPool.Return(psea as ChatSendEventArgs);
                        else if (psea is PluginChannleSendEventArgs)
                            PluginChannleEventArgsPool.Return(psea as PluginChannleSendEventArgs);
                        else
                            PacketEventArgsPool.Return(psea);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                NyaProxy.Logger.Exception(e);
                NyaProxy.GlobalQueueToken.Cancel();
            }
        }
    }
}
