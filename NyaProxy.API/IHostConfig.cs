using NyaProxy.API.Enum;
using System.Collections.Generic;
using System.Net;

namespace NyaProxy.API
{
    public interface IHostConfig
    {
        ServerFlags Flags { get; }
        ServerSelectMode SelectMode { get; }
        ForwardMode ForwardMode { get; }
        List<EndPoint> ServerEndPoints { get; }
        int CompressionThreshold { get; }
        int ProtocolVersion { get; }
    }
}
