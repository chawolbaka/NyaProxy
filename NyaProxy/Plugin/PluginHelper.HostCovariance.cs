using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NyaProxy.API;
using NyaProxy.Configs;

namespace NyaProxy.Plugin
{
    internal partial class PluginHelper
    {
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
        }

    }
}
