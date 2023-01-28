using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Buffers;
using MinecraftProtocol.IO.Pools;

namespace NyaProxy.Bridges
{
    public static class TransportLayerRepeater
    {
        private static IPool<SocketAsyncEventArgs> SocketAsyncEventArgsPool = new SocketAsyncEventArgsPool();
        private static IPool<AsyncUserToken> AsyncUserTokenPool =  new ObjectPool<AsyncUserToken>();
        private const int DEFUALT_RECEIVE_BUFFER_SIZE = 1024 * 8;
        public static void Create(Socket source, Socket destination, CancellationTokenSource token = default)
        {
            CancellationTokenSource InternalToken = token ??= new CancellationTokenSource();
            
            IMemoryOwner<byte> SourceReceiveBuffer = MemoryPool<byte>.Shared.Rent(DEFUALT_RECEIVE_BUFFER_SIZE);
            IMemoryOwner<byte> DestinationReceiveBuffer = MemoryPool<byte>.Shared.Rent(DEFUALT_RECEIVE_BUFFER_SIZE);
            SocketAsyncEventArgs SourceReceiveEventArgs = SocketAsyncEventArgsPool.Rent();
            SocketAsyncEventArgs SourceSendEventArgs = SocketAsyncEventArgsPool.Rent();

            SourceReceiveEventArgs.Completed += IO_Completed;
            SourceReceiveEventArgs.SetBuffer(SourceReceiveBuffer.Memory);
            SourceReceiveEventArgs.RemoteEndPoint = source.RemoteEndPoint;
            SourceReceiveEventArgs.UserToken = AsyncUserTokenPool.Rent().Setup(destination, SourceSendEventArgs, InternalToken);
            

            SourceSendEventArgs.Completed += IO_Completed;
            SourceSendEventArgs.RemoteEndPoint = destination.RemoteEndPoint;
            SourceSendEventArgs.UserToken = AsyncUserTokenPool.Rent().Setup(source, SourceReceiveEventArgs, InternalToken);


            SocketAsyncEventArgs DestinationReceiveEventArgs = SocketAsyncEventArgsPool.Rent();
            SocketAsyncEventArgs DestinationSendEventArgs = SocketAsyncEventArgsPool.Rent();

            DestinationReceiveEventArgs.Completed += IO_Completed;
            DestinationReceiveEventArgs.SetBuffer(DestinationReceiveBuffer.Memory);
            DestinationReceiveEventArgs.RemoteEndPoint = destination.RemoteEndPoint;
            DestinationReceiveEventArgs.UserToken = AsyncUserTokenPool.Rent().Setup(source, DestinationSendEventArgs, InternalToken);

            DestinationSendEventArgs.Completed += IO_Completed;
            DestinationSendEventArgs.RemoteEndPoint = source.LocalEndPoint;
            DestinationSendEventArgs.UserToken = AsyncUserTokenPool.Rent().Setup(destination, DestinationReceiveEventArgs, InternalToken);

            InternalToken.Token.Register(SourceReceiveBuffer.Dispose);
            InternalToken.Token.Register(DestinationReceiveBuffer.Dispose);

            if (!source.ReceiveAsync(SourceReceiveEventArgs))
                Task.Run(() => ProcessReceive(SourceReceiveEventArgs));

            if (!destination.ReceiveAsync(DestinationReceiveEventArgs))
                ProcessReceive(DestinationReceiveEventArgs);
        }


        private static void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            if (userToken == null || userToken.CancellationToken == null || userToken.CancellationToken.IsCancellationRequested)
                return;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    userToken.EventArgs.SetBuffer(e.MemoryBuffer.Slice(0, e.BytesTransferred));
                    if (!userToken.Socket.SendAsync(userToken.EventArgs))
                    {
                        ProcessSend(userToken.EventArgs);
                    }
                }
                else
                {
                    userToken.CancellationToken?.Cancel();
                }

            }
            catch (SocketException) { userToken.CancellationToken?.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        private static void ProcessSend(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = e.UserToken as AsyncUserToken;
            if (userToken == null || userToken.CancellationToken == null || userToken.CancellationToken.IsCancellationRequested)
                return;
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    if (!userToken.Socket.ReceiveAsync(userToken.EventArgs))
                    {
                        ProcessReceive(userToken.EventArgs);
                    }
                }
                else
                {
                    userToken.CancellationToken?.Cancel();
                }
            }
            catch (SocketException) { userToken.CancellationToken?.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        private static void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        private class AsyncUserToken : IDisposable
        {
            private bool _disposed = false;
            private Socket _socket;
            private SocketAsyncEventArgs _eventArgs;
            private CancellationTokenSource _cancellationToken;

            public AsyncUserToken() { }
            public AsyncUserToken Setup(Socket socket, SocketAsyncEventArgs eventArgs, CancellationTokenSource cancellationToken)
            {
                _disposed = false;
                _socket = socket ?? throw new ArgumentNullException(nameof(socket));
                _eventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));
                _cancellationToken = cancellationToken;
                _cancellationToken.Token.Register(Dispose);
                return this;
            }

            public Socket Socket => ThrowIfDisposed(_socket);
            public SocketAsyncEventArgs EventArgs => ThrowIfDisposed(_eventArgs);
            public CancellationTokenSource CancellationToken => ThrowIfDisposed(_cancellationToken);

            public void Dispose()
            {
                lock (this)
                {
                    bool disposed = _disposed;
                    _disposed = true;
                    if (!disposed)
                    {
                        try
                        {
                            //Socket.Shutdown(SocketShutdown.Send);
                            Socket.Close();
                        }
                        catch (SocketException) { }
                        catch (ObjectDisposedException) { }

                        _eventArgs.Completed -= IO_Completed;
                        SocketAsyncEventArgsPool.Return(_eventArgs);
                        AsyncUserTokenPool.Return(this);
                    }
                }
            }


            ~AsyncUserToken()
            {
                Dispose();
            }

            private T ThrowIfDisposed<T>(T value)
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().FullName);
                return value;
            }
        }
    }
}
