using NyaProxy.API.Enum;

namespace NyaProxy.Configs.Rule
{
    public interface IHostTargetRule
    {
        int CompressionThreshold { get; set; }
        ServerFlags Flags { get; set; }
        int ProtocolVersion { get; set; }
    }
}
