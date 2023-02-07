using NyaProxy.API.Enum;
using System.Collections.Generic;
using System.Net;

namespace NyaProxy.API
{
    public interface IHostConfig
    {
        string Name { get; }
        List<EndPoint> ServerEndPoints { get; }
        ServerFlags Flags { get; }
        ServerSelectMode SelectMode { get; }
        ForwardMode ForwardMode { get; }
        int CompressionThreshold { get; }
        int ProtocolVersion { get; }
    }
}
