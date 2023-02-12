using System;

namespace NyaProxy.API
{
    public abstract class Config
    {
        //UniqueId会直接作为key，如果使用属性有变动的可能性，所以这边使用readonly来保证初始化后就不可变。
        public readonly string UniqueId;

        public Config(string uniqueId)
        {
            UniqueId = uniqueId;
        }   
    }
}
