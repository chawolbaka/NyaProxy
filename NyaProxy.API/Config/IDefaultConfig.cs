namespace NyaProxy.API
{
    /// <summary>
    /// 如果实现该接口，那么在文件不存在时会在调用<see cref="SetDefault"/>后生成一份默认的配置文件
    /// </summary>
    public interface IDefaultConfig
    {
        /// <summary>
        /// 设定默认值，用于生成默认的配置文件
        /// </summary>
        void SetDefault();
    }
}
