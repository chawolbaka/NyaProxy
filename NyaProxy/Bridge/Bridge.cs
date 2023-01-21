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
        public Guid SessionId { get; }

        public Socket Source { get; set; }

        public Socket Destination { get; set; }

        protected CancellationTokenSource ListenToken = new CancellationTokenSource();

        public Bridge(Socket source, Socket destination)
        {
            SessionId = Guid.NewGuid();
            Source = source;
            Destination = destination;
            NyaProxy.Bridges.Add(SessionId, this);
        }

        public abstract IBridge Build();
        
        private static Func<Socket, EndPoint?> GetField_Socket_remoteEndPoint = ExpressionTreeUtils.CreateGetFieldMethodFormInstance<Socket, EndPoint?>("_remoteEndPoint");
        public virtual void Break()
        {
            lock (this)
            {
                try
                {
                    if (!NyaProxy.Bridges.ContainsKey(SessionId))
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
                    if (NyaProxy.Bridges.ContainsKey(SessionId))
                    {
                        NyaProxy.Bridges.Remove(SessionId);
                    }
                }
            }
        }


        protected virtual void SimpleExceptionHandle(object sender, PacketListener.UnhandledExceptionEventArgs e)
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
