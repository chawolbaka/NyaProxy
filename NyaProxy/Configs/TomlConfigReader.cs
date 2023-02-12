using NyaProxy.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tomlet;
using Tomlet.Models;

namespace NyaProxy.Configs
{
    public class TomlConfigReader : ConfigReader
    {
        private ConfigObject _configObject;
        private TomlTable _tomlTable;
        public override string FileType => "toml";

        public TomlConfigReader(string file) : this(TomlParser.ParseFile(file)) { }
        public TomlConfigReader(TomlTable tomlTable)
        {
            _configObject = ReadObject(tomlTable);
            _tomlTable = tomlTable;
        }

        public override bool ContainsKey(string key)
        {
            return _tomlTable.ContainsKey(key);
        }

        public override ConfigNode ReadProperty(string key)
        {
            return _configObject[key];
        }

        private ConfigObject ReadObject(TomlTable tomlTable)
        {
            ConfigObject table = new ConfigObject();
            foreach (var item in tomlTable.Entries)
            {
                if (item.Value is TomlTable tt)
                    table.Add(item.Key, ReadObject(tt));
                else if (item.Value is TomlArray ta)
                    table.Add(item.Key, ReadArray(ta));
                else
                    table.Add(item.Key, ReadNode(item.Value));
            }
            return table;
        }

        private ConfigArray ReadArray(TomlArray array)
        {
            ConfigArray configArray = new ConfigArray();
            configArray.Value.AddRange(array.ArrayValues.Select(x => ReadNode(x)));
            return configArray;
        }

        private ConfigNode ReadNode(TomlValue tomlValue)
        {
            if (tomlValue is TomlBoolean TB)
                return new BooleanNode(TB.Value);
            else if (tomlValue is TomlLong TL)
                return new NumberNode(TL.Value);
            else if (tomlValue is TomlDouble TD)
                return new DoubleNode(TD.Value);
            else if (tomlValue is TomlLocalDate TLD)
                return new DateTimeNode(TLD.Value);
            else if (tomlValue is TomlLocalTime TLT) //这什么奇怪的类型，规范里面怎么找不到...
                return new NumberNode(TLT.Value.Milliseconds);
            else if (tomlValue is TomlLocalDateTime TLDT)
                return new DateTimeNode(TLDT.Value);
            else if (tomlValue is TomlOffsetDateTime TODT)
                return new DateTimeNode(TODT.Value.DateTime);
            else if (tomlValue is TomlString TS)
                return new StringNode(TS.Value);
            else 
                throw new InvalidCastException($"Unknow toml value {tomlValue.GetType()}");
        }

    }
}
