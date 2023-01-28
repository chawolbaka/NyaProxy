using MinecraftProtocol.Compression;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Utils;
using NyaProxy.API.Enum;
using NyaProxy.EventArgs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace NyaProxy
{
    public partial class BlockingBridge : Bridge
    {
        private static BlockingCollection<SendEventArgs> SendQueue = new();
        private static BlockingCollection<PacketSendEventArgs>[] ReceiveQueues;


        private static ObjectPool<SendEventArgs> SendEventArgsPool = new();
        private static ObjectPool<PacketSendEventArgs> PacketEventArgsPool = new ();
        private static ObjectPool<ChatSendEventArgs> ChatEventArgsPool = new();
        private static ObjectPool<PluginChannleSendEventArgs> PluginChannleEventArgsPool = new();
        private static ObjectPool<AsyncChatEventArgs> AsyncChatEventArgsPool = new();
        private static CancellationTokenSource GlobalQueueToken = new CancellationTokenSource();
        private static ManualResetEvent SendSignal = new ManualResetEvent(false); //我那奇怪的CPU占有率不至于是因为这边导致的吧...


        internal static void Enqueue(Socket socket, Memory<byte> data) => SendQueue.Add(SendEventArgsPool.Rent().Setup(socket, data,null));
        internal static void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable) => SendQueue.Add(SendEventArgsPool.Rent().Setup(socket,  data, disposable));

        internal static void Setup(int networkThread) 
        {
            GlobalQueueToken.Token.Register(() => SendSignal.Set());
            if (ReceiveQueues != null)
            {
                foreach (var queue in ReceiveQueues)
                {
                    queue.Dispose();
                }
                ReceiveQueues = null;
            }

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

            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += (sender,e) => SendSignal.Set();
            Thread sendThread = new Thread(() => {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => Crash.Report(e.ExceptionObject as Exception);
                SendQueueHandler(eventArgs);
            });
            sendThread.IsBackground = true;
            sendThread.Name = "Network IO #0";
            sendThread.Start();
        }

        private static int LastIndex;
        private static SpinLock IndexLock = new SpinLock();
        public static int GetQueueIndex()
        {
            int result = 0; bool lockTaken = false;
            try
            {
                IndexLock.Enter(ref lockTaken);
                if (LastIndex + 1 >= ReceiveQueues.Length)
                    LastIndex = 0;
                else
                    result = ++LastIndex;
            }
            finally
            {
                if (lockTaken)
                    IndexLock.Exit();
            }

            return result;
        }

        private static void SendQueueHandler(SocketAsyncEventArgs e)
        {
            SendEventArgs sea = default; //必须在这边，否则每次循环都会创建一个空的
            while (!GlobalQueueToken.IsCancellationRequested)
            {
                try
                {
                    sea = SendQueue.Take(GlobalQueueToken.Token);
                    int send = 0, dataLength = sea.Data.Length;
                    
                    do
                    {
                        if (send > 0)
                            e.SetBuffer(sea.Data.Slice(send));
                        else
                            e.SetBuffer(sea.Data);

                        if (sea.Socket.SendAsync(e))
                        {
                            SendSignal.WaitOne();
                            SendSignal.Reset();
                            if (GlobalQueueToken.IsCancellationRequested)
                            {
                                SendSignal?.Dispose();
                                return;
                            }
                        }

                        if (e.SocketError != SocketError.Success || (e.BytesTransferred <= 0 && !NetworkUtils.CheckConnect(sea.Socket)))
                            break;
                        else
                            send += e.BytesTransferred;

                    } while (send < dataLength);

                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode is SocketError.ConnectionRefused or SocketError.ConnectionReset)
                        NyaProxy.Logger.Warn(se.Message);
                    else
                        NyaProxy.Logger.Error(se.Message);
                }
                finally
                {
                    sea.Dispose();
                    SendEventArgsPool.Return(sea);
                }
            }
        }

        private static DateTime LastTime = DateTime.Now; //简单处理一下重复的聊天信息，之后考虑更复杂的情况
        private static void ReceiveQueueHandler(PacketSendEventArgs e)
        {
            try
            {
                if (e._bridge.Source == null || e._bridge.Destination == null || !e._bridge.Source.Connected || !e._bridge.Destination.Connected)
                    return;
#if DEBUG
                if (true)
#else
                if (NyaProxy.Plugin.Count > 0)
#endif
                {
                    try
                    {
                        if (e is ChatSendEventArgs csea)
                        {
                            EventUtils.InvokeCancelEvent(e.Direction == Direction.ToClient ? NyaProxy.ChatMessageSendToClient : NyaProxy.ChatMessageSendToServer, e._bridge, csea);
                            var handler = NyaProxy.ChatMessageSened;
                            if (handler != null && (DateTime.Now - LastTime).TotalMilliseconds > 50)
                            {
                                LastTime = DateTime.Now;
                                if (ServerChatMessagePacket.TryRead(csea.Packet, out ServerChatMessagePacket scmp))
                                {
                                    var eventArgs = AsyncChatEventArgsPool.Rent().Setup(csea.ReceivedTime, scmp.Message);
                                    scmp?.Dispose();
                                    EventUtils.InvokeCancelEventAsync(handler, null, eventArgs).ContinueWith(x => AsyncChatEventArgsPool.Return(eventArgs));
                                }
                            }
                        }
                        else if (e is PluginChannleSendEventArgs pcsea)
                        {
                            //频道消息虽然有专门的处理系统，但如果需要还是能直接通过数据包发送事件来处理的，并且如果阻断了那么也不会触发频道消息的那一套系统
                            EventUtils.InvokeCancelEvent(e.Direction == Direction.ToClient ? NyaProxy.PacketSendToClient : NyaProxy.PacketSendToServer, e._bridge, e);
                            if (!e.IsBlock && NyaProxy.Channles.ContainsKey(pcsea.ChannleName))
                            {
                                Channle channle = NyaProxy.Channles[pcsea.ChannleName] as Channle;
                                channle?.Trigger(e.Direction == Direction.ToServer ? Side.Client : Side.Server, new ByteReader(pcsea.Data), e._bridge);
                            }
                        }
                        else
                        {
                            EventUtils.InvokeCancelEvent(e.Direction == Direction.ToClient ? NyaProxy.PacketSendToClient : NyaProxy.PacketSendToServer, e._bridge, e);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is SocketException)
                            throw;
                        NyaProxy.Logger.Exception(ex);
                    }
                }

                if (!e.IsBlock)
                {
                    if (!e._bridge.OverCompression && !e.PacketCheaged)
                    {
                        var rawData = e._eventArgs.RawData.Span;
                        for (int i = 0; i < rawData.Length; i++)
                        {
                            if (rawData[i].Length > 0)
                                Enqueue(e.Destination, rawData[i], i + 1 < e._eventArgs.RawData.Length ? null : e._eventArgs);
                        }
                    }
                    else
                    {
                        Memory<byte> data = e.Packet.Pack();
                        Enqueue(e.Destination, data, e._eventArgs);
                    }
                }
                else
                {
                    e._eventArgs?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Crash.Report(ex, true, false, false);
                e._bridge.Break();
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
                while (!GlobalQueueToken.IsCancellationRequested)
                {
                    action(queue.Take(GlobalQueueToken.Token));
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                GlobalQueueToken.Cancel();
                NyaProxy.Logger.Exception(e);
            }
        }

        private class SendEventArgs
        {
            public Socket Socket;
            public Memory<byte> Data;
            private IDisposable _disposable;

            public SendEventArgs() { }
            public SendEventArgs Setup(Socket socket, Memory<byte> data, IDisposable disposable)
            {
                Socket = socket;
                Data = data;
                _disposable = disposable;
                return this;
            }

            public void Dispose()
            {
                _disposable?.Dispose();
            }
        }
    }
}
