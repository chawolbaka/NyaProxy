using NyaFirewall.Rules;
using NyaProxy.API.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaFirewall
{
    [ConfigFile("config")]
    public class FirewallConfig : Config, IManualConfig, IDefaultConfig
    {
        public RuleAction DefaultAction { get; set; }

        public FirewallConfig() : base("FirewallConfig")
        {

        }

        public void Read(ConfigReader reader)
        {
            DefaultAction = Enum.Parse<RuleAction>(reader.ReadStringProperty("default-action"));
        }

        public void Write(ConfigWriter writer)
        {
            writer.WriteProperty("default-action", DefaultAction.ToString());
        }

        public void SetDefault()
        {
            DefaultAction = RuleAction.Block;
        }

    }
}
