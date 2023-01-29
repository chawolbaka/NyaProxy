using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.API.Channle;
using NyaProxy.Configs;
using NyaProxy.Extension;
using NyaProxy.Bridges;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Utils;
using System.Text.Json;

namespace NyaProxy.Plugin
{
    public class PluginManager : IEnumerable<PluginController>
    {
        private static SHA512 SHA512 = SHA512.Create();
        private static MethodInfo SetupPlugin = typeof(NyaPlugin).GetMethod("Setup", BindingFlags.Instance | BindingFlags.NonPublic);

        internal Dictionary<string, PluginController> Plugins = new Dictionary<string, PluginController>();
        internal ILogger Logger;

        public int Count => Plugins.Count;

        public PluginController this[string id] => Plugins.ContainsKey(id) ? Plugins[id] : null;
        public bool Contains(string id) => Plugins.ContainsKey(id);
        public IEnumerator<PluginController> GetEnumerator() => Plugins.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Plugins.Values.GetEnumerator();


        public PluginManager(ILogger logger)
        {
            Logger = logger;
        }

        public async Task<bool> LoadAsync(string directory)
        {
            directory = Path.Combine(Environment.CurrentDirectory, directory);
            if (!Directory.GetFiles(directory).Any(f => f.EndsWith("Manifest.json")))
                return false;

            try
            {
                Manifest manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(directory, "Manifest.json")));

                //检查必选项
                if (string.IsNullOrWhiteSpace(manifest.UniqueId))
                    throw new PluginLoadException(i18n.Plugin.UniqueId_Empty);
                if (string.IsNullOrWhiteSpace(manifest.Name))
                    throw new PluginLoadException(i18n.Plugin.Name_Empty);
                if (string.IsNullOrWhiteSpace(manifest.EntryDll))
                    throw new PluginLoadException(i18n.Plugin.EntryDll_Empty);
                if (manifest.Version == null)
                    throw new PluginLoadException(i18n.Plugin.Version_Empty);

                //检查要求的版本号是否高于当前程序的版本号
                if (manifest.MinimumApiVersion != null && NyaPlugin.ApiVersion < manifest.MinimumApiVersion)
                {
                    NyaProxy.Logger.Warn(i18n.Plugin.MinimumApiVersion_NotMatch.Replace("{CurrentApiVersion}", NyaPlugin.ApiVersion, "{UniqueId}", manifest.UniqueId, "{PluginName}", manifest.Name, "{MinimumApiVersion}", manifest.MinimumApiVersion));
                    return false;
                }

                //检查插件Id是否已存在
                if (Plugins.ContainsKey(manifest.UniqueId))
                {
                    NyaProxy.Logger.Warn(i18n.Plugin.UniqueId_Duplicate.Replace("{UniqueId}", manifest.UniqueId));
                    return false;
                }

                //检查文件可用性
                string EntryDllFile = Path.Combine(directory, manifest.EntryDll);
                if (!File.Exists(EntryDllFile))
                {
                    throw new PluginLoadException(i18n.Plugin.EntryDll_CannotFound.Replace("{FileName}", manifest.EntryDll));
                }
                else if (!string.IsNullOrWhiteSpace(manifest.Checksum))
                {
                    using FileStream cfs = new FileStream(EntryDllFile, FileMode.Open);
                    if (manifest.Checksum.ToUpper() != CryptoUtils.GetHexString(SHA512.ComputeHash(cfs)).ToUpper())
                        throw new PluginLoadException(i18n.Plugin.Checksum_Failed);
                }

                //开始加载插件
                using FileStream fs = new FileStream(EntryDllFile, FileMode.Open);
                AssemblyLoadContext context = new AssemblyLoadContext(manifest.EntryDll, true);
                foreach (Type type in context.LoadFromStream(fs).GetExportedTypes())
                {
                    if (type.BaseType != null && type.BaseType.Equals(typeof(NyaPlugin)))
                    {
                        NyaPlugin plugin = (NyaPlugin)Activator.CreateInstance(type);

                        Logger.Info(i18n.Plugin.Load_Success.Replace("{Name}", manifest.Name));
                        SetupPlugin.Invoke(plugin, new object[] {
                                new Action<string,Command>((source,command) => NyaProxy.CommandManager.Register(source,command)),
                                new PluginHelper(directory), Logger, manifest });
                        await plugin.OnEnable();
                        Plugins.Add(manifest.UniqueId, new PluginController(plugin, context, this, directory));
                        return true;
                    }
                }
            }
            catch (PluginLoadException pe)
            {
                Logger.Error(pe.Message);
            }
            catch (Exception e)
            {
                Logger.Error(i18n.Plugin.Load_Error.Replace("{File}", new DirectoryInfo(directory).Name));
                Logger.Exception(e);
                return false;
            }
            return false;
        }

        internal class PluginHelper : IPluginHelper
        {
            public DirectoryInfo WorkDirectory { get; }
            public IEvents Events => _events;
            public IConfigHelper Config => _configHelper;
            public INetworkHelper Network => _networkHelper;
            public IChannleContainer Channles => NyaProxy.Channles;
            public IReadOnlyDictionary<string, IHost> Hosts => new HostCovariance(NyaProxy.Config.Hosts);

            private static PluginEvents _events = new PluginEvents();
            private static NetworkHelper _networkHelper = new NetworkHelper();
            internal ConfigHelper _configHelper;


            //帮忙写配置文件
            public PluginHelper(string workDirectory)
            {
                WorkDirectory = new DirectoryInfo(workDirectory ?? throw new ArgumentNullException(nameof(workDirectory)));
                _configHelper = new ConfigHelper(workDirectory);
            }

            internal class ConfigHelper : IConfigHelper
            {
                public string WorkDirectory { get; }
                public List<ITomlConfig> ConfigList = new List<ITomlConfig>();
                public ConfigHelper(string workDirectory)
                {
                    WorkDirectory = workDirectory;
                }

                public void Register(ITomlConfig config, string fileName)
                {
                    config.File = new FileInfo(Path.Combine(WorkDirectory, fileName));
                    try
                    {
                        if (config.File.Exists && config.File.Length > 0)
                            config.Reload();
                        else
                            config.Save();
                    }
                    catch (NullReferenceException) { }
                    finally
                    {

                        if (config.File.Exists)
                            //用于全局重载和保存
                            ConfigList.Add(config);
                    }

                }
            }

            private class NetworkHelper : INetworkHelper
            {
                public void Enqueue(Socket socket, ICompatiblePacket packet)
                {
                    BlockingBridge.Enqueue(socket, packet.Pack());
                }

                public void Enqueue(Socket socket, Memory<byte> data)
                {
                    BlockingBridge.Enqueue(socket, data);
                }

                public void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable)
                {
                    BlockingBridge.Enqueue(socket, data, disposable);
                }

            }

            private class PluginEvents : IEvents
            {
                public ITransportEvent Transport => _transport;
                private static ITransportEvent _transport = new TransportEvent();
                private class TransportEvent : ITransportEvent
                {
                    public event AsyncCommonEventHandler<object, IAsyncChatEventArgs> ChatMessageSened { add => NyaProxy.ChatMessageSened += value; remove => NyaProxy.ChatMessageSened -= value; }

                    public event EventHandler<IConnectEventArgs> Connecting { add => NyaProxy.Connecting += value; remove => NyaProxy.Connecting -= value; }
                    public event EventHandler<IHandshakeEventArgs> Handshaking { add => NyaProxy.Handshaking += value; remove => NyaProxy.Handshaking -= value; }
                    public event EventHandler<ILoginStartEventArgs> LoginStart { add => NyaProxy.LoginStart += value; remove => NyaProxy.LoginStart -= value; }
                    public event EventHandler<ILoginSuccessEventArgs> LoginSuccess { add => NyaProxy.LoginSuccess += value; remove => NyaProxy.LoginSuccess -= value; }
                    public event EventHandler<IPacketSendEventArgs> PacketSendToClient { add => NyaProxy.PacketSendToClient += value; remove => NyaProxy.PacketSendToClient -= value; }
                    public event EventHandler<IPacketSendEventArgs> PacketSendToServer { add => NyaProxy.PacketSendToServer += value; remove => NyaProxy.PacketSendToServer -= value; }
                    public event EventHandler<IChatSendEventArgs> ChatMessageSendToClient { add => NyaProxy.ChatMessageSendToClient += value; remove => NyaProxy.ChatMessageSendToClient -= value; }
                    public event EventHandler<IChatSendEventArgs> ChatMessageSendToServer { add => NyaProxy.ChatMessageSendToServer += value; remove => NyaProxy.ChatMessageSendToServer -= value; }
                    public event EventHandler<IDisconnectEventArgs> Disconnected { add => NyaProxy.Disconnected += value; remove => NyaProxy.Disconnected -= value; }
                }
            }

            private class Host : IHost
            {
                public string Name { get; }

                public IHostConfig Config => config;
                public IReadOnlyDictionary<Guid, IBridge> Bridges { get; }
                private HostConfig config;

                public Host(string host, HostConfig config)
                {
                    Name = host;
                    this.config = config;
                    Bridges = NyaProxy.Bridges[host] as IReadOnlyDictionary<Guid, IBridge>;
                }
            }

            private class HostCovariance : IReadOnlyDictionary<string, IHost>
            {
                public IHost this[string key] => new Host(key, _hosts[key]);

                public IEnumerable<string> Keys => _hosts.Keys;

                public IEnumerable<IHost> Values => _hosts.Select((x) => new Host(x.Key, x.Value));

                public int Count => _hosts.Count;

                private IDictionary<string, HostConfig> _hosts;
                public HostCovariance(IDictionary<string, HostConfig> hosts)
                {
                    _hosts = hosts;
                }

                public bool ContainsKey(string key)
                {
                    return _hosts.ContainsKey(key);
                }

                public IEnumerator<KeyValuePair<string, IHost>> GetEnumerator()
                {
                    foreach (var host in _hosts)
                    {
                        yield return new KeyValuePair<string, IHost>(host.Key, new Host(host.Key, host.Value));
                    }
                }

                public bool TryGetValue(string key, [MaybeNullWhen(false)] out IHost value)
                {
                    bool canGet = _hosts.TryGetValue(key, out var host);
                    value = new Host(key, host);
                    return canGet;
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }

        }
    }
}
