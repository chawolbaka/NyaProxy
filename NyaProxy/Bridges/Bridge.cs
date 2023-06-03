using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.EventArgs;
using NyaProxy.Extension;
using NyaProxy.Debug;
using MinecraftProtocol.IO;
using MinecraftProtocol.Utils;

namespace NyaProxy.Bridges
{
    public abstract class Bridge : IBridge
    {
        internal static long Count => Interlocked.Read(ref _count);
        internal static long CurrentSequence => Interlocked.Read(ref _sequence);

        public long SessionId { get; }
        public Host Host { get; }
        public string HandshakeAddress { get; }
        public Socket Source { get; set; }
        public Socket Destination { get; set; }
    
        protected CancellationTokenSource ListenToken = new CancellationTokenSource();
        protected virtual string BreakMessage => $"{GetType().Name} breaked ({Source._remoteEndPoint()}<->{Destination._remoteEndPoint()})";

        private SpinLock _breakLock = new SpinLock();
        private bool _isBreaked; 
        private static long _count = 0;
        private static long _sequence = 1000; //从0开始的话StringTable那边看着有点太短，这边设置成1000仅仅是为了看着舒服点。

        internal static long GetNextSessionId()
        {
            Interlocked.CompareExchange(ref _sequence, 1000, int.MaxValue);
            return Interlocked.Increment(ref _sequence);
        }

        public Bridge(Host host, string handshakeAddress, Socket source, Socket destination) : this(GetNextSessionId(), host, handshakeAddress, source, destination) { }
        internal Bridge(long sessionId, Host host, string handshakeAddress, Socket source, Socket destination)
        {
            SessionId = sessionId;
            HandshakeAddress = handshakeAddress;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
            Host = host ?? throw new ArgumentNullException(nameof(host));

            ListenToken.Token.Register(Break);
            NyaProxy.Bridges?.TryAdd(SessionId, this);
            host.Bridges?.TryAdd(SessionId, this);

            _count = Interlocked.Increment(ref _count);
        }


        public abstract IBridge Build();

        public virtual void Break()
        {
            bool lockTaken = false;
            try
            {
                _breakLock.Enter(ref lockTaken);
                if (_isBreaked)
                    return;

                _count = Interlocked.Decrement(ref _count);
                _isBreaked = true;
            }
            finally
            {
                if (lockTaken)
                    _breakLock.Exit();
            }

            try
            {
                
                if (!NyaProxy.Bridges.ContainsKey(SessionId))
                    return;
                ListenToken?.Cancel();
                Task.Run(async () => 
                {
                    //直接断开socket可能会导致有遗言发不出来。
                    await Task.Delay(1000);
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
                });
                NyaProxy.Disconnected.Invoke(this, new DisconnectEventArgs(SessionId, Host), NyaProxy.Logger);
                NyaProxy.Logger.Info(BreakMessage);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                NyaProxy.Bridges.TryRemove(SessionId, out _);
                Host.Bridges.TryRemove(SessionId, out _);
            }
        }

        protected virtual void SimpleExceptionHandle(object sender, UnhandledIOExceptionEventArgs e)
        {
            e.Handled = true;
            if (e.Exception is OverflowException && NetworkUtils.CheckConnect(Source) && NetworkUtils.CheckConnect(Destination))
            {
                NyaProxy.Logger.Exception(e.Exception);
                return;
            }
            else if (e.Exception is not ObjectDisposedException && e.Exception is not SocketException && e.Exception is not OverflowException)
            {
                Crash.Report(e.Exception, true, true, false);
                Break();
            }
            else
            {
                (sender as IDisposable)?.Dispose();
                Break();
            }
        }
    }
}
