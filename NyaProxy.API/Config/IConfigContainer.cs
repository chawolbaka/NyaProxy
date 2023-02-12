using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public interface IConfigContainer
    {
        T Get<T>(int index) where T : Config;
        T Get<T>(string name) where T : Config;
        
        int Register(Type config);
        int Register(Type config, string fileName);
    }
}
