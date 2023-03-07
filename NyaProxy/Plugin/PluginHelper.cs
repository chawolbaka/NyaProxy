using System;
using System.IO;
using NyaProxy.API;
using NyaProxy.API.Channle;

namespace NyaProxy.Plugin
{
    internal partial class PluginHelper : IPluginHelper
    {
        public DirectoryInfo WorkDirectory { get; }
        public IEvents Events => _events;
        public IConfigContainer Config => _configContainer;
        public ICommandContainer Command => _commandContainer;
        public INetworkHelper Network => _networkHelper;
        public IChannleContainer Channles => NyaProxy.Channles;
        public IHostContainer Hosts => new HostCovariance(NyaProxy.Hosts);


        private static PluginEvents _events = new PluginEvents();
        private static NetworkHelper _networkHelper = NyaProxy.Network;
        private ConfigContainer _configContainer;
        private CommandContainer _commandContainer;

        public PluginHelper(string workDirectory, IManifest manifest)
        {
            WorkDirectory = new DirectoryInfo(workDirectory ?? throw new ArgumentNullException(nameof(workDirectory)));
            _commandContainer = new CommandContainer(manifest);
            _configContainer  = new ConfigContainer(workDirectory, manifest, _commandContainer);
        }
    }
}
