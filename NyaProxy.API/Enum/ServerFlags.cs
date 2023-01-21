using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Enum
{
    [Flags]
    public enum ServerFlags
    {
        None       = 0x00,
        OnlineMode = 0x01,
        Forge      = 0x02
    }
}
