namespace NyaProxy.API
{
    public abstract class ConfigReader
    {
        public virtual ConfigNode this[string key] => ReadProperty(key);
        
        public abstract bool ContainsKey(string key);

        public abstract string FileType { get; }

        public abstract ConfigNode  ReadProperty(string key); 
        public virtual BooleanNode  ReadBooleanProperty(string key)  => (BooleanNode)ReadProperty(key);
        public virtual NumberNode   ReadNumberProperty(string key)   => (NumberNode)ReadProperty(key);
        public virtual FloatNode    ReadFloatProperty(string key)    => (FloatNode)ReadProperty(key);
        public virtual DoubleNode   ReadDoubleProperty(string key)   => (DoubleNode)ReadProperty(key);
        public virtual DateTimeNode ReadDateTimeProperty(string key) => (DateTimeNode)ReadProperty(key);
        public virtual StringNode   ReadStringProperty(string key)   => (StringNode)ReadProperty(key);
        public virtual ConfigArray  ReadArray(string key)            => (ConfigArray)ReadProperty(key);
        public virtual ConfigObject ReadObject(string key)           => (ConfigObject)ReadProperty(key);
    }
}
