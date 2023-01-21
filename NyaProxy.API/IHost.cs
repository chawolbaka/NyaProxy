using NyaProxy.API.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public interface IHost
    {
        string Name { get; }

        ServerFlags Flags { get; }
        ServerSelectMode SelectMode { get; }
        ForwardMode ForwardMode { get; }
        List<EndPoint> ServerEndPoints { get; }

        int CompressionThreshold { get; }
        int ProtocolVersion { get; }
    }
}
