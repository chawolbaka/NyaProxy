namespace NyaProxy.API
{
    public class ConfigProperty
    {
        public string Key { get; set; }
        public ConfigNode Node { get; set; }
        public ConfigProperty(string key, ConfigNode node)
        {
            Key = key;
            Node = node;
        }
        public override string ToString() => Node.ToString();
        public override int GetHashCode() => Key.GetHashCode();
        public override bool Equals(object obj) => obj is ConfigProperty cp && cp.Key == Key;
    }
}
