using System;
using System.Collections;
using System.Collections.Generic;
using NyaProxy.API.Config.Nodes;

namespace NyaProxy.API.Config
{
    public abstract partial class ConfigReader : IEnumerable<ConfigProperty>
    {
        public abstract string FileType { get; }

        public abstract int Count { get; }

        public virtual ConfigNode this[string key] => ReadProperty(key);
        
        public abstract bool ContainsKey(string key);

        public abstract ConfigNode  ReadProperty(string key);


        public virtual BooleanNode ReadBooleanProperty(string key) => (BooleanNode)ReadProperty(key);
        public virtual bool TryReadBoolean(string key, out BooleanNode result)
        {
            result = null;
            if (!ContainsKey(key))
                return false;

            result = ReadProperty(key) as BooleanNode;
            return result != null;
        }

        public virtual bool TryReadBoolean(string key, out bool result)
        {
            result = default;
            if (!ContainsKey(key))
                return false;

            ConfigNode node = ReadProperty(key);
            if (node is BooleanNode)
            {
                result = ((BooleanNode)node).Value;
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual NumberNode ReadNumberProperty(string key) => (NumberNode)ReadProperty(key);
        public virtual bool TryReadNumber(string key, out NumberNode result)
        {
            result = null;
            if (!ContainsKey(key))
                return false;

            result = ReadProperty(key) as NumberNode;
            return result != null;
        }

        public virtual bool TryReadNumber(string key, out long result)
        {
            result = default;
            if (!ContainsKey(key))
                return false;

            ConfigNode node = ReadProperty(key);
            if (node is NumberNode)
            {
                result = ((NumberNode)node).Value;
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual DoubleNode ReadDoubleProperty(string key) => (DoubleNode)ReadProperty(key);
        public virtual bool TryReadDouble(string key, out DoubleNode result)
        {
            result = null;
            if (!ContainsKey(key))
                return false;

            result = ReadProperty(key) as DoubleNode;
            return result != null;
        }

        public virtual bool TryReadDouble(string key, out double result)
        {
            result = default;
            if (!ContainsKey(key))
                return false;

            ConfigNode node = ReadProperty(key);
            if (node is DoubleNode)
            {
                result = ((DoubleNode)node).Value;
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual DateTimeNode ReadDateTimeProperty(string key) => (DateTimeNode)ReadProperty(key);
        public virtual bool TryReadDateTime(string key, out DateTimeNode result)
        {
            result = null;
            if (!ContainsKey(key))
                return false;

            result = ReadProperty(key) as DateTimeNode;
            return result != null;
        }

        public virtual bool TryReadDateTime(string key, out DateTime result)
        {
            result = default;
            if (!ContainsKey(key))
                return false;

            ConfigNode node = ReadProperty(key);
            if (node is DateTimeNode)
            {
                result = ((DateTimeNode)node).Value;
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual StringNode ReadStringProperty(string key) => (StringNode)ReadProperty(key);
        public virtual bool TryReadString(string key, out StringNode result)
        {
            result = null;
            if (!ContainsKey(key))
                return false;

            result = ReadProperty(key) as StringNode;
            return result != null;
        }

        public virtual bool TryReadString(string key, out string result)
        {
            result = default;
            if (!ContainsKey(key))
                return false;

            ConfigNode node = ReadProperty(key);
            if (node is StringNode)
            {
                result = ((StringNode)node).Value;
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual ArrayNode ReadArrayProperty(string key) => (ArrayNode)ReadProperty(key);
        public virtual bool TryReadArray(string key, out ArrayNode result)
        {
            result = null;
            if (!ContainsKey(key))
                return false;

            result = ReadProperty(key) as ArrayNode;
            return result != null;
        }
        public virtual ObjectNode ReadObjectProperty(string key) => (ObjectNode)ReadProperty(key);
        public virtual bool TryReadObject(string key, out ObjectNode result)
        {
            result = null;
            if (!ContainsKey(key))
                return false;

            result = ReadProperty(key) as ObjectNode;
            return result != null;
        }


        public abstract IEnumerator<ConfigProperty> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
