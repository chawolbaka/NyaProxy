using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NyaProxy.API;
using NyaProxy.Configs;
using NyaProxy.Extension;
using MinecraftProtocol.Crypto;
using System.Text.Json;
using System.Threading;

namespace NyaProxy.Plugin
{
    public class PluginManager : IEnumerable<PluginController>
    {
        private static SHA512 SHA512 = SHA512.Create();
        private static MethodInfo SetupPlugin = typeof(NyaPlugin).GetMethod("Setup", BindingFlags.Instance | BindingFlags.NonPublic);

        internal Dictionary<string, PluginController> Plugins = new Dictionary<string, PluginController>();
        internal INyaLogger Logger;

        public int Count => Plugins.Count;

        public PluginController this[string id] => Plugins.ContainsKey(id) ? Plugins[id] : null;
        public bool Contains(string id) => Plugins.ContainsKey(id);
        public IEnumerator<PluginController> GetEnumerator() => Plugins.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Plugins.Values.GetEnumerator();


        public PluginManager(INyaLogger logger)
        {
            Logger = logger;
        }

        public async Task<bool> LoadAsync(string directory)
        {
            directory = Path.Combine(Environment.CurrentDirectory, directory);
            if (!Directory.GetFiles(directory).Any(f => f.EndsWith("Manifest.json")))
                return false;
            Manifest manifest = null;
            PluginController pluginController = null;
            try
            {
                manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(directory, "Manifest.json")));

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
                        pluginController = new PluginController(plugin, context, this, directory);
                        SetupPlugin.Invoke(plugin, new object[] { new PluginHelper(directory, manifest), Logger, manifest });

                        await plugin.OnEnable();
                        Logger.Info(i18n.Plugin.Load_Success.Replace("{Name}", manifest.Name));
                        Plugins.Add(manifest.UniqueId, pluginController);
                        return true;
                    }
                }
            }
            catch (PluginLoadException pe)
            {
                Logger.Error(pe.Message);
                if (pluginController != null)
                    await pluginController.UnloadAsync();
            }
            catch (Exception e)
            {
                Logger.Error(i18n.Plugin.Load_Error.Replace("{File}", manifest != null ? manifest.Name : new DirectoryInfo(directory).Name));
                Logger.Exception(e);

                if (pluginController != null)
                    await pluginController.UnloadAsync();
                return false;
            }
            return false;
        }
    }
}
