using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Enum
{
    [Flags]
    public enum Side
    {
        Client = 0x01,
        Server = 0x02
    }
}
