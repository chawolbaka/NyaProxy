using MinecraftProtocol.IO;
using MinecraftProtocol.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NyaProxy.API;
using NyaProxy.EventArgs;
using NyaProxy.Extension;
using NyaProxy.Configs;
using Ionic.Zlib;
using NyaProxy.Debug;

namespace NyaProxy.Bridges
{
    public abstract class Bridge : IBridge
    {
        internal static long Sequence => Interlocked.Read(ref _sequence);
        private static long _sequence = 1000; //从0开始的话StringTable那边看着有点太短，这边设置成1000仅仅是为了看着舒服点。

        public long SessionId { get; }

        public string HandshakeAddress { get; }

        public Socket Source { get; set; }

        public Socket Destination { get; set; }

        protected CancellationTokenSource ListenToken = new CancellationTokenSource();

        protected string HostName { get; }
        
        private SpinLock _breakLock = new SpinLock();
        private bool _isBreaked;

        public Bridge(string host, string handshakeAddress, Socket source, Socket destination)
        {
            SessionId = Interlocked.Increment(ref _sequence);
            HandshakeAddress = handshakeAddress;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
            HostName = host ?? throw new ArgumentNullException(nameof(host));

            ListenToken.Token.Register(Break);
            NyaProxy.Bridges[host]?.TryAdd(SessionId, this);
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

                _isBreaked = true;
            }
            finally
            {
                if (lockTaken)
                    _breakLock.Exit();
            }
            

            try
            {
                if (!NyaProxy.Bridges[HostName].ContainsKey(SessionId))
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
                NyaProxy.Logger.Info($"{GetType().Name} breaked ({Source._remoteEndPoint()}<->{Destination._remoteEndPoint()})");
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                if (NyaProxy.Bridges[HostName].ContainsKey(SessionId))
                {
                    NyaProxy.Bridges[HostName].TryRemove(SessionId, out _);
                    //如果该host已被删除，那么就由最后一个移除连接的Bridge来从Bridges中移除该host
                    if (!NyaProxy.Hosts.ContainsKey(HostName) && NyaProxy.Bridges[HostName].IsEmpty)
                        NyaProxy.Bridges.TryRemove(HostName, out _);
                }
            }

        }

        protected virtual void SimpleExceptionHandle(object sender, UnhandledIOExceptionEventArgs e)
        {
            e.Handled = true;
            if (e.Exception is ZlibException or OverflowException && NetworkUtils.CheckConnect(Source) && NetworkUtils.CheckConnect(Destination))
            {
                NyaProxy.Logger.Error(i18n.Error.PacketDecompressFailed);
                return;
            }
            else if (e.Exception is not ObjectDisposedException or SocketException or ZlibException or OverflowException)
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
