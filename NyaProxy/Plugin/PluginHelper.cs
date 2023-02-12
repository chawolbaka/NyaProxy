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
            _commandContainer = new CommandContainer(manifest);
            _configContainer = new ConfigContainer(workDirectory, manifest, _commandContainer);
        }

        internal class CommandContainer : ICommandContainer
        {
            public readonly IManifest Manifest;
            public readonly bool IsRoot;

            public CommandContainer(IManifest manifest)
            {
                Manifest = manifest;
                IsRoot = manifest.CommandPrefixes == null || manifest.CommandPrefixes.Count == 0;
                if (!IsRoot)
                {
                    foreach (var prefix in Manifest.CommandPrefixes)
                    {
                        NyaProxy.CommandManager.Register(new RootCommand(prefix));
                    }
                }
            }

            public void Register(Command command)
            {
                if (!IsRoot)
                {
                    foreach (var prefix in Manifest.CommandPrefixes)
                    {
                        try
                        {
                            NyaProxy.CommandManager.RegisteredCommands[prefix].RegisterChild(command);
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
            private CommandContainer _commandContainer;
            private bool _isConfigCommandRegistered;

            public ConfigContainer(string workDirectory, IManifest manifest, CommandContainer commandContainer)
            {
                WorkDirectory = workDirectory;
                _manifest = manifest;
                _commandContainer = commandContainer;
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
                    //如果配置文件不存在但该类实现了IDefaultConfig接口那么就创建默认的配置文件
                    if (!File.Exists(path))
                    {
                        config = (Config)Activator.CreateInstance(configType);
                        if (config is IDefaultConfig idc) 
                        {
                            idc.SetDefault();
                            SaveAsync(config,path).Wait();
                        }
                    }
                    else
                    {
                        config = LoadConfig(configType, path);
                    }

                    ConfigFiles.Add(config);
                    ConfigIdDictionary.Add(config.UniqueId, config);
                    ConfigPathDictionary.Add(config.UniqueId, path);


                    //如果插件配置了CommandPrefixes那么就注册子命令config来重载和保存配置文件
                    if (!_isConfigCommandRegistered&&!_commandContainer.IsRoot) 
                    {
                        _commandContainer.Register(new ConfigCommand(this));
                        _isConfigCommandRegistered = true;
                    }
                              
                    return ConfigFiles.Count - 1;
                }
                catch (Exception e)
                {
                    NyaProxy.Logger.Error(i18n.Error.LoadConfigFailed.Replace("{File}", fileName));
                    NyaProxy.Logger.Exception(e);
                    return -1;
                }
            }

            private Config LoadConfig(Type configType,string path)
            {
                //如果实现了IManualConfig接口那么就通过read来读取配置文件，否则就通过反序列化读取
                if (typeof(IManualConfig).IsAssignableFrom(configType))
                {
                    Config config = (Config)Activator.CreateInstance(configType);
                    (config as IManualConfig).Read(new TomlConfigReader(path));
                    return config;
                }
                else
                {
                    return (Config)TomletMain.To(configType, TomlParser.ParseFile(path));
                }
            }

            private void Reload(int index)
            {
                Config config = ConfigFiles[index];
                config = LoadConfig(config.GetType(), ConfigPathDictionary[config.UniqueId]);
                ConfigFiles[index] = config;
                ConfigIdDictionary[config.UniqueId] = config;
            }

            private void Reload(string uniqueId)
            {
                Reload(ConfigFiles.IndexOf(ConfigIdDictionary[uniqueId]));
            }

            private void ReloadAll()
            {
                for (int i = 0; i < ConfigFiles.Count; i++)
                {
                    Reload(i);
                }
            }

            internal async Task SaveAsync(int index)
            {
                Config config = ConfigFiles[index];
                await SaveAsync(config, ConfigPathDictionary[config.UniqueId]);
            }
            
            internal async Task SaveAsync(string uniqueId)
            {
                Config config = ConfigIdDictionary[uniqueId];
                await SaveAsync(config, ConfigPathDictionary[config.UniqueId]);
            }

            private async Task SaveAsync(Config config, string path)
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
                    await File.WriteAllTextAsync(path, document.SerializedValue, Encoding.UTF8);
                }
            }

            private async Task SaveAllAsync()
            {
                for (int i = 0; i < ConfigFiles.Count; i++)
                {
                    await SaveAsync(i);
                }
            }

            public T Get<T>(int index) where T : Config
            {
                try
                {
                    return (T)ConfigFiles[index];
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public T Get<T>(string uniqueId) where T : Config
            {
                try
                {
                    return (T)ConfigIdDictionary[uniqueId];
                }
                catch (Exception)
                {
                    return null;
                }
            }

            private class ConfigCommand : Command
            {
                public override string Name => "config";

                public override string Usage => "";

                public override string Description => "";

                private ConfigContainer _configContainer;

                private static readonly IEnumerable<string> _firstTabList = new List<string>() { "reload", "save" };

                public ConfigCommand(ConfigContainer configContainer)
                {
                    _configContainer = configContainer ?? throw new ArgumentNullException(nameof(configContainer));
                }

                public override async Task ExecuteAsync(ReadOnlyMemory<string> args, ICommandHelper helper)
                {
                    if (args.Length == 1)
                    {
                        switch (args.Span[0])
                        {
                            case "reload": _configContainer.ReloadAll(); break;
                            case "save": await _configContainer.SaveAllAsync(); break;
                            default: helper.Logger.Unpreformat($"Unknow operate {args.Span[0]}"); break;
                        }
                    }
                    else if(args.Length >= 2)
                    {
                        string uniqueId = args.Span[1];
                        if (!_configContainer.ConfigIdDictionary.ContainsKey(uniqueId))
                            helper.Logger.Unpreformat($"{uniqueId} cannot be found.");
                        else
                            switch (args.Span[0])
                            {
                                case "reload": _configContainer.Reload(args.Span[1]); break;
                                case "save": await _configContainer.SaveAsync(args.Span[1]); break;
                                default: helper.Logger.Unpreformat($"Unknow operate {args.Span[0]}"); break;
                            }
                    }
                }

                public override IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args)
                {
                    if (args.Length == 0)
                        return _firstTabList;
                    else if (args.Length == 1)
                        return _configContainer.ConfigFiles.Select(x => x.UniqueId);
                    else
                        return Enumerable.Empty<string>();
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
