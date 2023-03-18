using System;

namespace NyaProxy.API
{
    public abstract partial class ConfigWriter
    {
        public abstract string FileType { get; }

        public abstract ConfigWriter WriteProperty(string key, ConfigNode node);
        public virtual ConfigWriter WriteProperty(string key, BooleanNode node)  => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, NumberNode node)   => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, FloatNode node)    => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, DoubleNode node)   => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, DateTimeNode node) => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, StringNode node)   => WriteProperty(key, (ConfigNode)node);
    }
}
