using System;

namespace NyaProxy.API
{
    public abstract partial class ConfigWriter
    {
        public abstract string FileType { get; }

        public abstract ConfigWriter WriteProperty(string key, ConfigNode node);
        public virtual ConfigWriter WriteProperty(string key, BooleanNode node) => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, bool value) => WriteProperty(key, (ConfigNode)new BooleanNode(value));
        public virtual ConfigWriter WriteProperty(string key, NumberNode node) => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, long value) => WriteProperty(key, (ConfigNode)new NumberNode(value));
        public virtual ConfigWriter WriteProperty(string key, FloatNode node) => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, float value) => WriteProperty(key, (ConfigNode)new FloatNode(value));
        public virtual ConfigWriter WriteProperty(string key, DoubleNode node) => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, double value) => WriteProperty(key, (ConfigNode)new DoubleNode(value));
        public virtual ConfigWriter WriteProperty(string key, DateTimeNode node) => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, DateTime value) => WriteProperty(key, (ConfigNode)new DateTimeNode(value));
        public virtual ConfigWriter WriteProperty(string key, StringNode node) => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, string value) => WriteProperty(key, (ConfigNode)new StringNode(value));
    }
}
