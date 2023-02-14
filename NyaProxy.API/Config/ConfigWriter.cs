using System;

namespace NyaProxy.API
{
    public abstract partial class ConfigWriter
    {
        public abstract string FileType { get; }

        public abstract ConfigWriter WriteProperty(string key, ConfigNode node);

    }
}
