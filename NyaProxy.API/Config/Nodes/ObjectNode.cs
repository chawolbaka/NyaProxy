using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NyaProxy.API.Config.Nodes
{
    public class ObjectNode : ConfigNode, IDictionary<string, ConfigNode>
    {
        public ConfigNode this[string key]
        {
            get => Nodes[key];
            set => Nodes[key] = value;
        }
        public Dictionary<string, ConfigNode> Nodes { get; set; }

        public int Count => Nodes.Count;

        public ObjectNode()
        {
            Nodes = new Dictionary<string, ConfigNode>();
        }

        public ObjectNode(IEnumerable<KeyValuePair<string, ConfigNode>> nodes)
        {
            Nodes = new Dictionary<string, ConfigNode>(nodes);
        }

        public void Add(string key, ConfigNode node) => Nodes.Add(key, node);

        public bool ContainsKey(string key) => Nodes.ContainsKey(key);

        public bool Remove(string key) => Nodes.Remove(key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out ConfigNode node) => Nodes.TryGetValue(key, out node);

        public void Clear() => Nodes.Clear();

        public IEnumerator<KeyValuePair<string, ConfigNode>> GetEnumerator() => Nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Nodes.GetEnumerator();

        ICollection<string> IDictionary<string, ConfigNode>.Keys => Nodes.Keys;

        ICollection<ConfigNode> IDictionary<string, ConfigNode>.Values => Nodes.Values;

        bool ICollection<KeyValuePair<string, ConfigNode>>.IsReadOnly => false;

        void ICollection<KeyValuePair<string, ConfigNode>>.Add(KeyValuePair<string, ConfigNode> item) => ((ICollection<KeyValuePair<string, ConfigNode>>)Nodes).Add(item);

        bool ICollection<KeyValuePair<string, ConfigNode>>.Contains(KeyValuePair<string, ConfigNode> item) => ((ICollection<KeyValuePair<string, ConfigNode>>)Nodes).Contains(item);

        void ICollection<KeyValuePair<string, ConfigNode>>.CopyTo(KeyValuePair<string, ConfigNode>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, ConfigNode>>)Nodes).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<string, ConfigNode>>.Remove(KeyValuePair<string, ConfigNode> item) => ((ICollection<KeyValuePair<string, ConfigNode>>)Nodes).Remove(item);

    }
}
