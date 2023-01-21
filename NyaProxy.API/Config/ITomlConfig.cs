using System.IO;
using Tommy;

namespace NyaProxy.API
{
    public interface ITomlConfig : ITomlTable
    {
        FileInfo File { get; set; }
        void Reload();
        void Save();
        //ITomlConfig SetupDefault();
    }
}