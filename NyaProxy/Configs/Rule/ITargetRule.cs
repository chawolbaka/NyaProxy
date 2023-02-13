namespace NyaProxy.Configs.Rule
{
    public interface ITargetRule
    {
        string Target { get; set; }
        TargetType Type { get; set; }
    }
}
