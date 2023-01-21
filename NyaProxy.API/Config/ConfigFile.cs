using System;
using System.IO;
using Tommy;

namespace NyaProxy.API
{
    public class ConfigFile : TomlTable, ITomlConfig
    {
        public virtual FileInfo File { get; set; }

        public ConfigFile() { }
        public ConfigFile(string file) : this(new FileInfo(file)) { }
        public ConfigFile(FileInfo file) : this(file, false) { }
        public ConfigFile(FileInfo file, bool load)
        {
            File = file;
            if (load)
                Reload();
        }

        public virtual void Reload()
        {
            if(File.Exists)
            {
                using var fs = File.OpenRead();
                using StreamReader reader = new StreamReader(fs);
                using var parser = new TOMLParser(reader) { ForceASCII = false };
                Clear();
                parser.Parse(this);
            }
        }

        public virtual void Save()
        {
            using FileStream fs = new FileStream(File.FullName, FileMode.OpenOrCreate, FileAccess.Write);
            using StreamWriter writer = new StreamWriter(fs);
            this.WriteTo(writer);
            writer.Flush();
        }

        public override int GetHashCode()
        {
            return File.FullName.GetHashCode();
        }
    }
}
