using NyaProxy.API.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analysis
{
    [ConfigFile("config")]
    public class AnalysisConfig : Config, IDefaultConfig
    {
        public int SinglePageLength { get; set; }

        public AnalysisConfig() : base("main")
        {
        }

        public void SetDefault()
        {
            SinglePageLength = 10;
        }
    }
}
