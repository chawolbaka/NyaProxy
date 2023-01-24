using MinecraftProtocol.IO;
using MinecraftProtocol.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NyaProxy.API;
using NyaProxy.EventArgs;

namespace NyaProxy
{
    public abstract class Bridge : IBridge
    {
        internal static long Sequence => Interlocked.Read(ref _sequence);
        private static long _sequence = 1000;

        public long SessionId { get; }

        public Socket Source { get; set; }

        public Socket Destination { get; set; }

        protected CancellationTokenSource ListenToken = new CancellationTokenSource();

        private HostConfig _host { get; }

        

        public Bridge(HostConfig host, Socket source, Socket destination)
        {
            SessionId = Interlocked.Increment(ref _sequence);
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
            _host = host ?? throw new ArgumentNullException(nameof(host));

            NyaProxy.Bridges[host.Name]?.TryAdd(SessionId, this);
        }

        public abstract IBridge Build();
        
        private static Func<Socket, EndPoint?> GetField_Socket_remoteEndPoint = ExpressionTreeUtils.CreateGetFieldMethodFormInstance<Socket, EndPoint?>("_remoteEndPoint");
        public virtual void Break()
        {
            lock (this)
            {
                try
                {
                    if (!NyaProxy.Bridges[_host.Name].ContainsKey(SessionId))
                        return;
                    ListenToken?.Cancel();
                    if (Source is not null && NetworkUtils.CheckConnect(Source))
                    {
                        Source.Shutdown(SocketShutdown.Both);
                        Source.Close();
                    }
                    if (Destination is not null && NetworkUtils.CheckConnect(Destination))
                    {
                        Destination.Shutdown(SocketShutdown.Both);
                        Destination.Close();
                    }
                    EventUtils.InvokeCancelEvent(NyaProxy.Disconnected, this, new DisconnectEventArgs());
                    NyaProxy.Logger.Info($"{GetType().Name} breaked ({GetField_Socket_remoteEndPoint(Source)}<->{GetField_Socket_remoteEndPoint(Destination)})");
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
                finally
                {
                    if (NyaProxy.Bridges[_host.Name].ContainsKey(SessionId))
                    {
                        NyaProxy.Bridges[_host.Name].TryRemove(SessionId, out _);
                        //如果该host已被删除，那么就由最后一个移除连接的Bridge来从Bridges中移除该host
                        if (!NyaProxy.Config.Hosts.ContainsKey(_host.Name) && NyaProxy.Bridges[_host.Name].IsEmpty)
                            NyaProxy.Bridges.TryRemove(_host.Name, out _);
                    }
                }
            }
        }


        protected virtual void SimpleExceptionHandle(object sender, UnhandledIOExceptionEventArgs e)
        {
            if (e.Exception is ObjectDisposedException || e.Exception is SocketException || (e.Exception is OverflowException && (!NetworkUtils.CheckConnect(Source) || ! NetworkUtils.CheckConnect(Destination))))
            {
                e.Handled = true;
                (sender as IDisposable)?.Dispose();
            }
            else
            {
                //写入错误信息流
            }
            //Break();
        }
    }
}
