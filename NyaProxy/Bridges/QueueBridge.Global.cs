using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using NyaProxy.API.Enum;
using NyaProxy.EventArgs;
using NyaProxy.Debug;
using MinecraftProtocol.IO.Pools;
using Microsoft.Extensions.Logging;

namespace NyaProxy.Bridges
{
    public partial class QueueBridge : Bridge
    {

        private static BlockingCollection<PacketSendEventArgs>[] ReceiveBlockingQueues;
        private static ConcurrentQueue<PacketSendEventArgs>[] ReceiveQueues;
        private static ObjectPool<PacketSendEventArgs> PacketEventArgsPool = new ();
        
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
                long lastBridgeId = -1;
                PacketSendEventArgs psea;
                BufferManager sendBuffer = null;
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
                            if (sendBuffer != null && sendBuffer.Push())
                                sendBuffer = null;
                            Thread.Sleep(200 / ((int)Bridge.Count + 1));
                        }
                        
                        if (NyaProxy.Config.EnableStickyPacket)
                        {
                            if (lastBridgeId > 0 && psea.Bridge.SessionId != lastBridgeId && sendBuffer != null && sendBuffer.Push())
                                sendBuffer = null;

                            lastBridgeId = psea.Bridge.SessionId;
                            sendBuffer = psea.Bridge._buffer;
                        }
                    }


                    try
                    {

                        //触发事件
                        try
                        {
                            if (psea is ChatSendEventArgs csea)
                                (psea.Direction == Direction.ToClient ? NyaProxy.ChatMessageSendToClient : NyaProxy.ChatMessageSendToServer).Invoke(psea.Bridge, csea, NyaProxy.Logger);
                            else
                                (psea.Direction == Direction.ToClient ? NyaProxy.PacketSendToClient : NyaProxy.PacketSendToServer).Invoke(psea.Bridge, psea, NyaProxy.Logger);


                        }
                        catch (Exception ex)
                        {
                            if (ex is SocketException)
                                psea.Bridge?.Break();
                            NyaProxy.Logger.LogError(ex);
                        }

                        //转发数据
                        if (!psea.IsBlock && psea.Bridge.Destination != null && psea.Bridge.Destination.Connected)
                        {
                            //如果数据没有被修改过那么就直接发送接收到的原始数据，避免Pack造成的内存分配。
                            if (psea.EventArgs != null && !psea.Bridge.IsOnlineMode && !psea.Bridge.OverCompression && !psea.PacketCheaged)
                            {
                                var rawDataBlock = psea.EventArgs.RawData.Span;
                                if (sendBuffer!=null && !EnableBlockingQueue && !psea.DestinationCheaged && rawDataBlock.Length == 1 && rawDataBlock[0].Length < NyaProxy.Config.StickyPacketLimit)
                                {
                                    /*
                                     * 主动粘连小包，减缓对内核的压力，在遇到以下条件时会发送缓存区内的数据
                                     * 1.非阻塞队列TryDequeue失败
                                     * 2.TryDequeue取出的Bridge和上一个不同
                                     * 3.加入的数据大于缓存区可用空间
                                     * 4.需要发送不满足粘包条件的数据包前
                                     */

                                    if (psea.Direction == Direction.ToClient)
                                        sendBuffer.Client.Add(rawDataBlock[0], psea.EventArgs);
                                    else
                                        sendBuffer.Server.Add(rawDataBlock[0], psea.EventArgs);
                                }
                                else
                                {
                                    if (sendBuffer != null && sendBuffer.Push())
                                        sendBuffer = null;

                                    for (int i = 0; i < rawDataBlock.Length; i++)
                                    {
                                        if (rawDataBlock[i].Length > 0)
                                            NyaProxy.Network.Enqueue(psea.Destination, rawDataBlock[i], i + 1 >= psea.EventArgs.RawData.Length ? psea.EventArgs : null);
                                    }
                                }

                            }
                            else
                            {
                                if (sendBuffer != null && sendBuffer.Push())
                                    sendBuffer = null;

                                if (psea.Direction == Direction.ToClient)
                                    NyaProxy.Network.Enqueue(psea.Destination, psea.Bridge.CryptoHandler.TryEncrypt(psea.Packet.Get().Pack()), (IDisposable)psea.EventArgs ?? psea.Packet);
                                else
                                    NyaProxy.Network.Enqueue(psea.Destination, psea.Packet.Get().Pack(), (IDisposable)psea.EventArgs ?? psea.Packet);
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
                        NyaProxy.Logger.LogError(ex);
                    }
                    finally
                    {
                        if (psea is PacketSendEventArgs)
                            PacketEventArgsPool.Return(psea);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                NyaProxy.Logger.LogError(e);
                NyaProxy.GlobalQueueToken.Cancel();
            }
        }
    }
}
