using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.API.Enum;
using NyaProxy.Extension;

namespace NyaProxy.Configs
{
    public class HostConfig : Config, IDefaultConfig, IManualConfig, IHostConfig
    {
        public string Name { get; set; }
        public ServerSelectMode SelectMode { get; set; }
        public ForwardMode ForwardMode { get; set; }
        public List<EndPoint> ServerEndPoints { get; set; }
        public int CompressionThreshold { get; set; }
        public int ProtocolVersion { get; set; }
        public ServerFlags Flags { get; set; }
        public bool TcpFastOpen { get; set; }
        public int ConnectionTimeout { get; set; }
        public int ConnectionThrottle { get; set; }

        private static Random Random = new Random();
        private SafeIndex _serverIndex;
        private DateTime _lastConnectTime = default;

        public HostConfig(string uniqueId) : base(uniqueId)
        {

        }

        public void Read(ConfigReader reader)
        {
            try
            {
                Name = reader.ReadStringProperty("host");
                Flags = Enum.Parse<ServerFlags>(reader.ReadStringProperty("server-flags"));
                SelectMode = Enum.Parse<ServerSelectMode>(reader.ReadStringProperty("server-select-mode"));
                ForwardMode = Enum.Parse<ForwardMode>(reader.ReadStringProperty("player-info-forwarding-mode"));
                ProtocolVersion = (int)reader.ReadNumberProperty("server-version");
                CompressionThreshold = reader.ContainsKey("compression-threshold") ? (int)reader.ReadNumberProperty("compression-threshold") : -1;
                TcpFastOpen = reader.ReadBooleanProperty("tcp-fast-open");
                ConnectionTimeout = (int)reader.ReadNumberProperty("connection-timeout");
                ConnectionThrottle = (int)reader.ReadNumberProperty("connection-throttle");

                List<EndPoint> serverEndPoints = new List<EndPoint>();
                foreach (StringNode server in reader.ReadArray("servers"))
                {
                    if (IPEndPoint.TryParse(server, out IPEndPoint ipEndPoint))
                    {
                        serverEndPoints.Add(ipEndPoint);
                    }
                    else
                    {
                        string[] s = server.Value.Split(':');
                        if (s.Length == 2 && ushort.TryParse(s[1], out ushort port))
                        {
                            serverEndPoints.Add(new DnsEndPoint(s[0], port));
                        }
                    }
                }
                _serverIndex = new SafeIndex(serverEndPoints.Count);
                ServerEndPoints = serverEndPoints;
            }
            catch (Exception e)
            {
                NyaProxy.Logger.Error(i18n.Error.LoadConfigFailed.Replace("{File}", $"{Name}.{reader.FileType}"));
                NyaProxy.Logger.Exception(e);
            }
        }

        public void Write(ConfigWriter writer)
        {
            try
            {
                writer.WriteProperty("host", new StringNode(i18n.Config.Host));

                ConfigArray servers = new ConfigArray();

                for (int i = 0; i < ServerEndPoints.Count; i++)
                {
                    if (ServerEndPoints[i] is DnsEndPoint dnsEndPoint)
                        servers.Value.Add(new StringNode($"{dnsEndPoint.Host}:{dnsEndPoint.Port}"));
                    else
                        servers.Value.Add(new StringNode(ServerEndPoints[i].ToString()));
                }
                writer.WriteArray("servers", servers);

                writer.WriteProperty("server-select-mode",          new StringNode(SelectMode.ToString(),i18n.Config.SelectMode));
                writer.WriteProperty("server-version",              new NumberNode(ProtocolVersion, i18n.Config.ProtocolVersion));
                writer.WriteProperty("server-flags",                new StringNode(Flags.ToString(), i18n.Config.ServerFlags));
                writer.WriteProperty("compression-threshold",       new NumberNode(CompressionThreshold, i18n.Config.CompressionThreshold));
                writer.WriteProperty("player-info-forwarding-mode", new StringNode(ForwardMode.ToString(), i18n.Config.ForwardMode));
                writer.WriteProperty("tcp-fast-open",               new BooleanNode(TcpFastOpen, i18n.Config.ClientTcpFastOpen));
                writer.WriteProperty("connection-timeout",          new NumberNode(ConnectionTimeout, i18n.Config.ConnectionTimeout));
                writer.WriteProperty("connection-throttle",         new NumberNode(ConnectionThrottle, i18n.Config.ConnectionThrottle));
            }
            catch (Exception e)
            {
                NyaProxy.Logger.Error(i18n.Error.SaveConfigFailed.Replace("{File}", $"{UniqueId}.{writer.FileType}"));
                NyaProxy.Logger.Exception(e);
            }
        }

        public void SetDefault()
        {
            Name = "example";
            ForwardMode = ForwardMode.Direct;
            SelectMode = ServerSelectMode.Failover;
            Flags = ServerFlags.None;
            ServerEndPoints = new List<EndPoint>() { new DnsEndPoint("example.net", 25565), new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 233) };
            ProtocolVersion = -1;
            CompressionThreshold = -1;
            TcpFastOpen = false;
            ConnectionTimeout = 1000 * 6;
            ConnectionThrottle = 2000;
        }

        
        public Task<Socket> OpenConnectAsync(EndPoint endPoint)
        {
            if (_lastConnectTime != default && (DateTime.Now - _lastConnectTime).TotalMilliseconds < ConnectionThrottle)
                throw new DisconnectException("连接频率过高");

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); ;
            if (TcpFastOpen)
            {
                if (OperatingSystem.IsWindows())
                    socket.EnableWindowsFastOpenClient();
                else if (OperatingSystem.IsLinux())
                    socket.EnableLinuxFastOpenConnect();
            }
            
            var tcs = new TaskCompletionSource<Socket>();
            var eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += (s, e) =>
            {
                if (e.SocketError == SocketError.Success)
                {
                    tcs.TrySetResult(socket);
                    _lastConnectTime = DateTime.Now;
                }
                else
                {
                    tcs.TrySetException(new SocketException((int)e.SocketError));
                }
            };

            eventArgs.RemoteEndPoint = endPoint;
            eventArgs.UserToken = tcs;

            if (!socket.ConnectAsync(eventArgs))
            {
                _lastConnectTime = DateTime.Now;
                return Task.FromResult(socket); 
            }
            else
            {
                return tcs.Task;
            }
        }
        public async Task<Socket> OpenConnectAsync(EndPoint endPoint, int timeout)
        {
            if (timeout < 0)
                return await OpenConnectAsync(endPoint);

            Task<Socket> task = OpenConnectAsync(endPoint);
            await Task.WhenAny(task, Task.Delay(timeout));
            if (!task.IsCompletedSuccessfully)
            {
                task.ContinueWith((t) => t.Result.Close());
                throw new SocketException((int)SocketError.TimedOut);
            }
            else
            {
                return await task;
            }
        }

        public async Task<Socket> OpenConnectAsync()
        {
            if (SelectMode == ServerSelectMode.Failover)
            {
                var servers = ServerEndPoints.ToArray();
                for (int i = 0; i < servers.Length; i++)
                {
                    try
                    {
                        return await OpenConnectAsync(servers[i], ConnectionTimeout);
                    }
                    catch (SocketException e)
                    {
                        if (i + 1 >= servers.Length)
                            throw new Exception(i18n.Error.NoSocketAvailable, e);
                    }
                }
            }
            else if (SelectMode == ServerSelectMode.Pool)
            {
                return await OpenConnectAsync(ServerEndPoints[_serverIndex.Get()], ConnectionTimeout);
            }
            else if (SelectMode == ServerSelectMode.Random)
            {
                return await OpenConnectAsync(ServerEndPoints[Random.Next(0, ServerEndPoints.Count - 1)], ConnectionTimeout);
            }

            throw new Exception(i18n.Error.NoSocketAvailable);
        }


        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
