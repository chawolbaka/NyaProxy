using System.Collections;
using System.Collections.Generic;

namespace NyaProxy.API
{
    public class ArrayNode : ConfigNode, IList<ConfigNode>
    {
        public List<ConfigNode> Value { get; set; }

        public int Count => Value.Count;

        public bool IsReadOnly => false;

        public ConfigNode this[int index] { get => Value[index]; set => Value[index] = value; }

        public ArrayNode()
        {
            Value = new List<ConfigNode>();
        }
        
        public ArrayNode(IEnumerable<ConfigNode> nodes)
        {
            Value = new List<ConfigNode>(nodes);
        }

        public int IndexOf(ConfigNode item) => Value.IndexOf(item);

        public bool Contains(ConfigNode item) => Value.Contains(item);

        public void Add(ConfigNode item) => Value.Add(item);

        public void AddRange(IEnumerable<ConfigNode> nodes) => Value.AddRange(nodes);

        public void Insert(int index, ConfigNode item) => Value.Insert(index, item);

        public bool Remove(ConfigNode item) => Value.Remove(item);

        public void RemoveAt(int index) => Value.RemoveAt(index);
        
        public void Clear() => Value.Clear();

        public void CopyTo(ConfigNode[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);        
        
        public IEnumerator<ConfigNode> GetEnumerator() => Value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Value.GetEnumerator();
        
    }
}
