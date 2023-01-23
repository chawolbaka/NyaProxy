using System;
using System.Collections.Generic;
using NyaProxy.API.Channle;

namespace NyaProxy.API
{
    public interface IHost
    {
        string Name { get; }

        IHostConfig Config { get; }

        IReadOnlyDictionary<Guid, IBridge> Bridges { get; }
    }
}
