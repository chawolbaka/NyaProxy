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
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Utils;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Chat;

namespace NyaProxy.Bridges
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


        internal static void Enqueue(Socket socket, Memory<byte> data) => SendQueue.Add(SendEventArgsPool.Rent().Setup(socket, data, null));
        internal static void Enqueue(Socket socket, Memory<byte> data, Action callback) => SendQueue.Add(SendEventArgsPool.Rent().Setup(socket, data, callback));
        internal static void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable) => SendQueue.Add(SendEventArgsPool.Rent().Setup(socket, data, disposable != null ? disposable.Dispose : null));


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
                    sea?.Callback?.Invoke();
                    SendEventArgsPool.Return(sea);
                }
            }
        }

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
                    //触发事件
                    try
                    {
                        if (e is ChatSendEventArgs csea)
                            EventUtils.InvokeCancelEvent(e.Direction == Direction.ToClient ? NyaProxy.ChatMessageSendToClient : NyaProxy.ChatMessageSendToServer, e._bridge, csea);
                        else
                            EventUtils.InvokeCancelEvent(e.Direction == Direction.ToClient ? NyaProxy.PacketSendToClient : NyaProxy.PacketSendToServer, e._bridge, e);
                        
                        if (!e.IsBlock && e is PluginChannleSendEventArgs pcsea && NyaProxy.Channles.ContainsKey(pcsea.ChannleName))
                        {
                            Channle channle = NyaProxy.Channles[pcsea.ChannleName] as Channle;
                            channle?.Trigger(e.Direction == Direction.ToServer ? Side.Client : Side.Server, new ByteReader(pcsea.Data), e._bridge);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is SocketException)
                            e._bridge?.Break();
                        NyaProxy.Logger.Exception(ex);
                    }
                }

                if (!e.IsBlock)
                {
                    //如果数据没有被修改过那么就直接发送接收到的原始数据，避免Pack造成的内存分配。
                    if (!e._bridge.IsOnlineMode && !e._bridge.OverCompression && !e.PacketCheaged)
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
                        if (e.Direction == Direction.ToClient)
                            Enqueue(e.Destination, e._bridge.CryptoHandler.TryEncrypt(e.Packet.Pack()), e._eventArgs);
                        else
                            Enqueue(e.Destination, e.Packet.Pack(), e._eventArgs);
                        
                    }
                }
                else
                {
                    e._eventArgs?.Dispose();
                }
            }
            catch (Exception ex)
            {
                e._bridge.Break();
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
            public Action Callback;

            public SendEventArgs() { }
            public SendEventArgs Setup(Socket socket, Memory<byte> data, Action callback)
            {
                Socket = socket;
                Data = data;
                Callback = callback;
                return this;
            }
        }
    }
}
