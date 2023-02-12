using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.API.Channle;
using NyaProxy.Configs;
using NyaProxy.Bridges;
using MinecraftProtocol.Packets;
using Tomlet;
using Tomlet.Models;
using System.Text;

namespace NyaProxy.Plugin
{
    internal class PluginHelper : IPluginHelper
    {
        public DirectoryInfo WorkDirectory { get; }
        public IEvents Events => _events;
        public IConfigContainer Config => _configContainer;
        public ICommandContainer Command => _commandContainer;
        public INetworkHelper Network => _networkHelper;
        public IChannleContainer Channles => NyaProxy.Channles;
        public IHostContainer Hosts => new HostCovariance(NyaProxy.Hosts);

        private static PluginEvents _events = new PluginEvents();
        private static NetworkHelper _networkHelper = new NetworkHelper();
        internal ConfigContainer _configContainer;
        internal CommandContainer _commandContainer;

        public PluginHelper(string workDirectory, IManifest manifest)
        {
            WorkDirectory = new DirectoryInfo(workDirectory ?? throw new ArgumentNullException(nameof(workDirectory)));
            _configContainer = new ConfigContainer(workDirectory, manifest);
            _commandContainer = new CommandContainer(manifest);
        }

        internal class CommandContainer : ICommandContainer
        {
            public IManifest Manifest;

            public CommandContainer(IManifest manifest)
            {
                Manifest = manifest;
                if (Manifest.CommandPrefixes != null && Manifest.CommandPrefixes.Count > 0)
                {
                    foreach (var prefix in Manifest.CommandPrefixes)
                    {
                        NyaProxy.CommandManager.Register(new RootCommand(prefix));
                    }
                }
            }

            public void Register(Command command)
            {
                if (Manifest.CommandPrefixes != null && Manifest.CommandPrefixes.Count > 0)
                {
                    foreach (var prefix in Manifest.CommandPrefixes)
                    {
                        try
                        {
                            NyaProxy.CommandManager.RegisteredCommands[prefix].AddChild(command);
                        }
                        catch (CommandRegisteredException cre)
                        {
                            NyaProxy.Logger.Error(i18n.Error.CommandRegistered.Replace("{CommandName}", cre.Command));
                        }
                    }
                }
                else
                {
                    NyaProxy.CommandManager.Register(command);
                }
            }

            private class RootCommand : Command
            {
                public override string Name { get; }

                public override string Usage => "";

                public override string Description => "";

                public RootCommand(string name)
                {
                    Name = name;
                }

                public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
                {
                    await ExecuteChildrenAsync(args, helper);
                }

                public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
                {
                    return GetChildrenTabCompletions(args);
                }
            }
        }

        internal class ConfigContainer : IConfigContainer
        {
            public string WorkDirectory { get; }

            public List<Config> ConfigFiles = new();

            public Dictionary<string, Config> ConfigIdDictionary = new();

            public Dictionary<string, string> ConfigPathDictionary = new();

            private IManifest _manifest;

            public ConfigContainer(string workDirectory, IManifest manifest)
            {
                WorkDirectory = workDirectory;
                _manifest = manifest;
            }

            public int Register(Type configType)
            {
                ConfigFileAttribute attribute = configType.GetCustomAttribute<ConfigFileAttribute>();
                if (attribute == null)
                    throw new NotImplementedException($"该类型未拥有{nameof(ConfigFileAttribute)}属性，请手动填写文件名");

                return Register(configType, attribute.FileName);
            }

            public int Register(Type configType, string fileName)
            {
                string path = Path.Combine(WorkDirectory, fileName);
                Config config;

                try
                {

                    if (!File.Exists(path))
                    {
                        config = (Config)Activator.CreateInstance(configType);
                        if (config is IDefaultConfig idc) //如果配置文件不存在但该类实现了IDefaultConfig接口那么就创建默认的配置文件
                        {
                            idc.SetDefault();
                            Save(path);
                        }
                    }
                    else if (typeof(IManualConfig).IsAssignableFrom(configType)) //如果实现了IManualConfig接口那么就通过read来读取配置文件，否则就通过反序列化读取
                    {
                        config = (Config)Activator.CreateInstance(configType);
                        if (config is IManualConfig imc)
                            imc.Read(new TomlConfigReader(path));
                    }
                    else
                    {
                        config = (Config)TomletMain.To(configType, TomlParser.ParseFile(path));
                    }

                    ConfigFiles.Add(config);
                    ConfigIdDictionary.Add(config.UniqueId, config);
                    ConfigPathDictionary.Add(config.UniqueId, path);

                    return ConfigFiles.Count - 1;
                }
                catch (Exception e)
                {
                    NyaProxy.Logger.Error(i18n.Error.LoadConfigFailed.Replace("{File}", fileName));
                    NyaProxy.Logger.Exception(e);
                    return -1;
                }

            }


            public void Save(int index)
            {
                Config config = ConfigFiles[index];
                Save(config, ConfigPathDictionary[config.UniqueId]);
            }
            public void Save(string name)
            {
                Config config = ConfigIdDictionary[name];
                Save(config, ConfigPathDictionary[config.UniqueId]);
            }

            private void Save(Config config, string path)
            {
                if (config is IManualConfig imc)
                {
                    TomlConfigWriter writer = new TomlConfigWriter();
                    imc.Write(writer);
                    writer.Save(path);
                }
                else
                {
                    TomlDocument document = TomletMain.DocumentFrom(config.GetType(), config);
                    document.ForceNoInline = true;
                    File.WriteAllText(path, document.SerializedValue, Encoding.UTF8);
                }
            }

            public T Get<T>(int index) where T : Config
            {
                return (T)ConfigFiles[index];
            }

            public T Get<T>(string name) where T : Config
            {
                return (T)ConfigIdDictionary[name];
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

        private class HostCovariance : IHostContainer
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
