using System;
using NyaProxy.API.Config.Nodes;

namespace NyaProxy.API.Config
{
    public abstract partial class ConfigWriter
    {
        public abstract string FileType { get; }

        public abstract ConfigWriter WriteProperty(string key, ConfigNode node);
        public virtual ConfigWriter WriteProperty(string key, BooleanNode node)  => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, NumberNode node)   => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, byte value)        => WriteProperty(key, new NumberNode(value));
        public virtual ConfigWriter WriteProperty(string key, sbyte value)       => WriteProperty(key, new NumberNode(value));
        public virtual ConfigWriter WriteProperty(string key, short value)       => WriteProperty(key, new NumberNode(value));
        public virtual ConfigWriter WriteProperty(string key, ushort value)      => WriteProperty(key, new NumberNode(value));
        public virtual ConfigWriter WriteProperty(string key, int value)         => WriteProperty(key, new NumberNode(value));
        public virtual ConfigWriter WriteProperty(string key, uint value)        => WriteProperty(key, new NumberNode(value));
        public virtual ConfigWriter WriteProperty(string key, long value)        => WriteProperty(key, new NumberNode(value));
        public virtual ConfigWriter WriteProperty(string key, DoubleNode node)   => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, DateTimeNode node) => WriteProperty(key, (ConfigNode)node);
        public virtual ConfigWriter WriteProperty(string key, StringNode node)   => WriteProperty(key, (ConfigNode)node);
    }
}
