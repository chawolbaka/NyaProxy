namespace NyaProxy.API
{
    public abstract class ConfigWriter
    {
        public abstract string FileType { get; }

        public abstract ConfigWriter WriteProperty(string key, ConfigNode node);
        public virtual ConfigWriter WriteObject(string key, ConfigObject @object) => WriteProperty(key, @object);
        public virtual ConfigWriter WriteArray(string key, ConfigArray array) => WriteProperty(key, array);
    }
}
