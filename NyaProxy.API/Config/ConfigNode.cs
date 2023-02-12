namespace NyaProxy.API
{
    public abstract partial class ConfigNode
    {
        public virtual ConfigComment Comment { get; set; }

        public override string ToString()
        {
            return GetType().GetProperty("Value").GetValue(this).ToString();
        }
    }
}
