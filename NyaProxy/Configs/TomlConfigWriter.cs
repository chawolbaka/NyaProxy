using NyaProxy.API;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;
using Tomlet;
using Tomlet.Models;

namespace NyaProxy.Configs
{
    public class TomlConfigWriter : ConfigWriter
    {
        public override string FileType => "toml";

        public readonly TomlDocument Document = TomlDocument.CreateEmpty();

        public override ConfigWriter WriteProperty(string key, ConfigNode node)
        {
            if (node is ConfigObject co)
                Document.Put(key, ConvertToTomlTable(co));
            else if (node is ConfigArray ca)
                Document.Put(key, ConvertToTomlArray(ca));
            else
                Document.Put(key, ConvertToTomlValue(node));
            return this;
        }

        public virtual void Save(string file)
        {
            Document.ForceNoInline = true;
            File.WriteAllText(file, Document.SerializedValue, Encoding.UTF8);
        }

        private TomlValue ConvertToTomlValue(ConfigNode node)
        {
            TomlValue tomlValue;
            if (node is BooleanNode BN)
                tomlValue = BN ? TomlBoolean.True : TomlBoolean.False;
            else if (node is NumberNode NN)
                tomlValue = new TomlLong(NN);
            else if (node is FloatNode FN)
                tomlValue = new TomlDouble(FN);
            else if (node is DoubleNode DN)
                tomlValue = new TomlDouble(DN);
            else if (node is DateTimeNode DTN)
                tomlValue = new TomlLocalDateTime(DTN);
            else if (node is StringNode SN)
                tomlValue = new TomlString(SN);
            else if (node is ConfigObject CO)
                tomlValue = ConvertToTomlTable(CO);
            else
                throw new InvalidCastException($"Unknow node {node.GetType()}");

            if (node.Comment != null)
            {
                tomlValue.Comments.PrecedingComment = node.Comment.Preceding;
                tomlValue.Comments.InlineComment = node.Comment.Inline;
            }
            return tomlValue;
        }

        private TomlTable ConvertToTomlTable(ConfigObject @object)
        {
            TomlTable tomlTable = new TomlTable();
            if (@object.Comment != null)
            {
                tomlTable.Comments.PrecedingComment = @object.Comment.Preceding;
                tomlTable.Comments.InlineComment = @object.Comment.Inline;
            }

            foreach (var node in @object.Nodes)
            {
                if (node.Value is ConfigObject co)
                    tomlTable.Put(node.Key, ConvertToTomlTable(co));
                else if (node.Value is ConfigArray ca)
                    tomlTable.Put(node.Key, ConvertToTomlArray(ca));
                else
                    tomlTable.Put(node.Key, ConvertToTomlValue(node.Value));
            }
            return tomlTable;
        }

        private TomlArray ConvertToTomlArray(ConfigArray array)
        {
            TomlArray tomlArray = new TomlArray();
            if (array.Comment != null)
            {
                tomlArray.Comments.PrecedingComment = array.Comment.Preceding;
                tomlArray.Comments.InlineComment = array.Comment.Inline;
            }

            tomlArray.ArrayValues.AddRange(array.Value.Select(x => ConvertToTomlValue(x)));
            return tomlArray;
        }
    }
}
