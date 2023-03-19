using System;
using System.Collections.Generic;
using System.Linq;
using NyaProxy.API.Config;
using NyaProxy.API.Config.Nodes;
using Tomlet;
using Tomlet.Models;

namespace NyaProxy.Configs
{
    public class TomlConfigReader : ConfigReader
    {
        private ObjectNode _configObject;
        private TomlTable _tomlTable;
        public override string FileType => "toml";
        public override int Count => _tomlTable.Entries.Count;


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

        public override IEnumerator<ConfigProperty> GetEnumerator()
        {
            return _configObject.Nodes.Select(e => new ConfigProperty(e.Key, e.Value)).GetEnumerator();
        }

        private ObjectNode ReadObject(TomlTable tomlTable)
        {
            ObjectNode table = new ObjectNode();
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

        private ArrayNode ReadArray(TomlArray array)
        {
            ArrayNode configArray = new ArrayNode();
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
            else if (tomlValue is TomlTable TT)
                return ReadObject(TT);
            else
                throw new InvalidCastException($"Unknow toml value {tomlValue.GetType()}");
        }

    }
}
