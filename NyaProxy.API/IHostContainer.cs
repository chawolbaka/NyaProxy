using System.Collections.Generic;

namespace NyaProxy.API
{
    public interface IHostContainer : IReadOnlyDictionary<string, IHost>
    {

    }
}
