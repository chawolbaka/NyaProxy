using System;

namespace NyaProxy.API
{
    public class ConfigFileAttribute : Attribute
    {
        public string FileName;

        public ConfigFileAttribute(string fileName)
        {
            FileName = fileName;
        }
    }
}
