using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NyaProxy.API;

namespace NyaProxy.Plugin
{
    internal partial class PluginHelper
    {
        private class HostCovariance : IHostContainer
        {
            public IHost this[string key] => _hosts[key];

            public IEnumerable<string> Keys => _hosts.Keys;

            public IEnumerable<IHost> Values => _hosts.Select((x) => x.Value);

            public int Count => _hosts.Count;

            private IDictionary<string, Host> _hosts;

            public HostCovariance(IDictionary<string, Host> hosts)
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
                    yield return new KeyValuePair<string, IHost>(host.Key, host.Value);
                }
            }

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out IHost value)
            {
                bool canGet = _hosts.TryGetValue(key, out var host);
                value = host;
                return canGet;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
