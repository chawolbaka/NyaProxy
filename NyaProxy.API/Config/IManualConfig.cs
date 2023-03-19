namespace NyaProxy.API.Config
{
    public interface IManualConfig
    {
        void Read(ConfigReader reader);
        void Write(ConfigWriter writer);
    }
}
