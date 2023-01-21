using MinecraftProtocol.DataType;

namespace NyaProxy.API
{
    public interface ILoginSuccessEventArgs : IPacketSendEventArgs
    {
        public UUID Id { get; set; }
        public string Name { get; set; }
    }
}