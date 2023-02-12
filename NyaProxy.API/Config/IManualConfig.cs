namespace NyaProxy.API
{
    public interface IManualConfig
    {
        void Read(ConfigReader reader);
        void Write(ConfigWriter writer);
    }
}
