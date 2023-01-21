namespace NyaProxy.API
{
    public interface IBlockEventArgs
    {
        bool IsBlock { get; }
        void Block();
    }
}