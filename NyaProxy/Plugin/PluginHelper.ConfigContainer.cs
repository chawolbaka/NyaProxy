using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.Configs;
using Tomlet;
using Tomlet.Models;
using System.Text;
using System.Xml;
using Microsoft.Win32;

namespace NyaProxy.Plugin
{
    internal partial class PluginHelper
    {
        internal class ConfigContainer : IConfigContainer
        {
            public string DefaultFileType => "toml";
            public string WorkDirectory { get; }

            public int Count => ConfigFiles.Count;
            public bool RegisterConfigCommand { get; set; }

            public List<Config> ConfigFiles = new();

            public Dictionary<string, Config> ConfigIdDictionary = new();

            public Dictionary<string, string> ConfigPathDictionary = new();
            private IManifest _manifest;
            private CommandContainer _commandContainer;
            private bool _isConfigCommandRegistered;

            public ConfigContainer(string workDirectory, IManifest manifest, CommandContainer commandContainer)
            {
                RegisterConfigCommand = true;
                WorkDirectory = workDirectory;
                _manifest = manifest;
                _commandContainer = commandContainer;
            }

            public int Register(Type configType)
            {
                ConfigFileAttribute attribute = configType.GetCustomAttribute<ConfigFileAttribute>();
                if (attribute == null)
                    throw new NotImplementedException($"该类型未拥有{nameof(ConfigFileAttribute)}属性，请手动填写文件名");

                if (!attribute.FileName.Contains('.'))
                    return Register(configType, attribute.FileName + "." + DefaultFileType);
                else
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
                            SaveAsync(config, path).Wait();
                        }
                    }
                    else
                    {
                        config = LoadConfig(configType, path);
                    }

                    return Register(config, path);
                }
                catch (Exception e)
                {
                    NyaProxy.Logger.Error(i18n.Error.LoadConfigFailed.Replace("{File}", fileName));
                    NyaProxy.Logger.Exception(e);
                    return -1;
                }
            }
            public int Register(Config config)
            {
                ConfigFileAttribute attribute = config.GetType().GetCustomAttribute<ConfigFileAttribute>();
                if (attribute == null)
                    throw new NotImplementedException($"该类型未拥有{nameof(ConfigFileAttribute)}属性，请手动填写文件名");

                if (!attribute.FileName.Contains('.'))
                    return Register(config, attribute.FileName + "." + DefaultFileType);
                else
                    return Register(config, attribute.FileName);
            }

            public int Register(Config config, string fileName)
            {
                try
                {
                    if (config == null)
                        throw new ArgumentNullException(nameof(config));


                    ConfigFiles.Add(config);
                    ConfigIdDictionary.Add(config.UniqueId, config);
                    ConfigPathDictionary.Add(config.UniqueId, Path.Combine(WorkDirectory, fileName));

                    //如果插件配置了CommandPrefixes那么就注册子命令config来重载和保存配置文件
                    if (RegisterConfigCommand && !_isConfigCommandRegistered && !_commandContainer.IsRoot)
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

            public bool Unregister(string uniqueId)
            {
                if (!ConfigIdDictionary.ContainsKey(uniqueId))
                    return false;
                Config config = ConfigIdDictionary[uniqueId];
                ConfigIdDictionary.Remove(config.UniqueId);
                ConfigPathDictionary.Remove(config.UniqueId);
                ConfigFiles.Remove(config);
                return true;
            }

            public bool Unregister(int index)
            {
                if (index > ConfigFiles.Count)
                    return false;
                Config config = ConfigFiles[index];
                ConfigIdDictionary.Remove(config.UniqueId);
                ConfigPathDictionary.Remove(config.UniqueId);
                ConfigFiles.RemoveAt(index);
                return true;
            }

            public void Clear()
            {
                ConfigFiles.Clear();
                ConfigIdDictionary.Clear();
                ConfigPathDictionary.Clear();
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

            public void Reload(int index)
            {
                if (index > ConfigFiles.Count)
                    return;

                Config config = null;
                try
                {
                    config = ConfigFiles[index];
                    config = LoadConfig(config.GetType(), ConfigPathDictionary[config.UniqueId]);
                    ConfigFiles[index] = config;
                    ConfigIdDictionary[config.UniqueId] = config;
                }
                catch (Exception e)
                {
                    NyaProxy.Logger.Error(i18n.Error.LoadConfigFailed.Replace("{File}", $"{config.UniqueId}.toml"));
                    NyaProxy.Logger.Exception(e);
                }
            }

            public void Reload(string uniqueId)
            {
                Reload(ConfigFiles.IndexOf(ConfigIdDictionary[uniqueId]));
            }

            public void ReloadAll()
            {
                for (int i = 0; i < ConfigFiles.Count; i++)
                {
                    Reload(i);
                }
            }

            public async Task SaveAsync(int index)
            {
                Config config = ConfigFiles[index];
                await SaveAsync(config, ConfigPathDictionary[config.UniqueId]);
            }
            
            public async Task SaveAsync(string uniqueId)
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
                    else if (args.Length == 1 && _configContainer.ConfigFiles.Count > 0)
                        return _configContainer.ConfigFiles.Select(x => x.UniqueId);
                    else
                        return Enumerable.Empty<string>();
                }
            }
        }

    }
}
