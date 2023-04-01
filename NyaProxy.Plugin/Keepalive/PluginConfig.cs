using System;
using NyaProxy.API.Config;

namespace Keepalive
{
    [ConfigFile("config")]
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
            writer.WriteProperty("timeout", Timeout);
        }

        public void SetDefault()
        {
            Timeout = 1000 * 80;
        }
    }
}
