using System.Collections;
using System.Collections.Generic;

namespace NyaProxy.API
{
    public abstract partial class ConfigReader : IEnumerable<ConfigProperty>
    {
        public abstract string FileType { get; }

        public abstract int Count { get; }

        public virtual ConfigNode this[string key] => ReadProperty(key);
        
        public abstract bool ContainsKey(string key);

        public abstract ConfigNode  ReadProperty(string key);
        

 

        public abstract IEnumerator<ConfigProperty> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
