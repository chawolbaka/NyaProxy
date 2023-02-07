using NyaProxy.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NyaProxy.Plugin
{
    public class PluginController
    {
        public NyaPlugin Plugin { get; private set; }
        private AssemblyLoadContext Context { get; }
        private PluginManager Manager;
        private string PluginDirectory;
        private SpinLock UnloadLock = new SpinLock();

        public PluginController(NyaPlugin plugin, AssemblyLoadContext context, PluginManager manager, string directory)
        {
            Plugin = plugin;
            Context = context;
            Manager = manager;
            PluginDirectory = directory;
        }

        public async Task ReloadAsync()
        {
            await UnloadAsync();
            await Manager.LoadAsync(PluginDirectory);
        }

        public async Task UnloadAsync()
        {
            NyaPlugin plugin;
            bool lockTaken = false;
            try
            {
                UnloadLock.Enter(ref lockTaken);
                plugin = Plugin;
                if (Plugin == null)
                    return;
                Plugin = null;
            }
            finally
            {
                if (lockTaken)
                    UnloadLock.Exit();
            }

            try
            {
                foreach (var file in (plugin.Helper as PluginManager.PluginHelper)?._configHelper?.ConfigList)
                {
                    try
                    {
                        file.Save();
                    }
                    catch (IOException e)
                    {
                        NyaProxy.Logger.Error(i18n.Error.SaveConfigFailed);
                        NyaProxy.Logger.Exception(e);
                    }
                }
                await plugin.OnDisable();
                Context.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Manager.Logger.Info(i18n.Plugin.Unload_Success.Replace("{Name}", plugin.Manifest.Name));
            }
            catch (Exception e)
            {
                Manager.Logger.Error(i18n.Plugin.Unload_Error.Replace("{Name}", plugin.Manifest.Name));
                Manager.Logger.Exception(e);
            }
            finally
            {
                if (Manager.Plugins.ContainsKey(plugin.Manifest.UniqueId))
                    Manager.Plugins.Remove(plugin.Manifest.UniqueId);
            }
        }
    }
}
