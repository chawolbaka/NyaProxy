using System;

namespace NyaProxy.Configs.Rule
{
    public class TargetRule : ITargetRule
    {
        public TargetType Type { get; set; }
        public string Target { get; set; }

        public TargetRule(TargetType type, string target)
        {
            Type = type;
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

    }
}
