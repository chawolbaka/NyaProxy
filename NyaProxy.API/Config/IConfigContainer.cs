using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public interface IConfigContainer
    {
        string DefaultFileType { get; }
        bool RegisterConfigCommand { get; set; }
        int Count { get; }

        T Get<T>(int index) where T : Config;
        T Get<T>(string name) where T : Config;
        
        int Register(Type config);
        int Register(Type config, string fileName);

        bool Unregister(string uniqueId);
        bool Unregister(int index);
        void Clear();
    }
}
