using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NyaProxy.API;

namespace Keepalive
{
    [ConfigFile("config.toml")]
    public class PluginConfig : Config, IDefaultConfig, IManualConfig
    {
        public virtual long Timeout { get; set; }

        public PluginConfig() : base("MainConfig")
        {

        }

        public void Read(ConfigReader reader)
        {
            Timeout = reader.ReadNumberProperty("timeout");
        }

        public void Write(ConfigWriter writer)
        {
            writer.WriteProperty("timeout", new NumberNode(Timeout));
        }

        public void SetDefault()
        {
            Timeout = 1000 * 80;
        }
    }
}
