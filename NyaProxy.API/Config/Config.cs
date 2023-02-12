using System;

namespace NyaProxy.API
{
    public abstract class Config
    {
        //UniqueId会直接作为key，如果使用属性有变动的可能性，所以这边使用readonly来保证初始化后就不可变。
        public readonly string UniqueId;

        /// <summary>
        /// 是否在程序退出时自动保存配置文件
        /// </summary>
        public virtual bool AutoSave { get; set; }

        public Config(string uniqueId)
        {
            UniqueId = uniqueId;
            AutoSave = true;
        }   
    }
}
