using System.Collections.Generic;
using NyaProxy.API;
using System.Collections.Concurrent;
using NyaProxy.Configs;
using NyaProxy.Bridges;

namespace NyaProxy
{
    public class Host : HostConfig, IHost
    {
        public ConcurrentDictionary<long, Bridge> Bridges { get; } = new ConcurrentDictionary<long, Bridge>();

        IReadOnlyDictionary<long, IBridge> IHost.Bridges => Bridges as IReadOnlyDictionary<long, IBridge>;

        public Host(string uniqueId) : base(uniqueId)
        {

        }
    }
}
