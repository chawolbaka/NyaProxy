using System;

namespace NyaProxy.API.Config
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
