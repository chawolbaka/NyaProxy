using NyaProxy.API.Enum;

namespace NyaProxy.Configs.Rule
{
    public class HostTargetRule : TargetRule, IHostTargetRule
    {
        public ServerFlags Flags { get; set; }
        public int ProtocolVersion { get; set; }
        public int CompressionThreshold { get; set; }

        public HostTargetRule(TargetType type, string target) : base(type, target)
        {
            ProtocolVersion = -1;
            CompressionThreshold = -1;
        }
    }
}
