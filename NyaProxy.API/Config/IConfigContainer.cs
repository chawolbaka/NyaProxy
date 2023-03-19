using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Config
{
    public interface IConfigContainer
    {
        string DefaultFileType { get; }
        bool RegisterConfigCommand { get; set; }
        int Count { get; }

        T Get<T>(int index) where T : Config;
        T Get<T>(string name) where T : Config;


        int Register(Config config);
        int Register(Config config, string fileName);
        
        int Register(Type config);
        int Register(Type config, string fileName);


        void Reload(int index);
        void Reload(string uniqueId);

        Task SaveAsync(int index);
        Task SaveAsync(string uniqueId);

        bool Unregister(string uniqueId);
        bool Unregister(int index);
        void Clear();
    }
}
