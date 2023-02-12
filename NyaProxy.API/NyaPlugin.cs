using System;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public abstract class NyaPlugin
    {
        /// <summary>
        /// 当前Api版本
        /// </summary>
        public static readonly Version ApiVersion = new Version(1, 0, 0, 0);

        public IManifest Manifest => _manifest;


        public IPluginHelper Helper => _helper;
        

        public ILogger Logger => _logger;

        private IManifest _manifest;
        private IPluginHelper _helper;
        private ILogger _logger;

        //该方法会被反射调用
        private void Setup(IPluginHelper helper, ILogger logger, IManifest manifest)
        {
            _helper = helper;
            _logger = logger;
            _manifest = manifest;
        }

        /// <summary>
        /// 插件被启用
        /// </summary>
        public abstract Task OnEnable();

        /// <summary>
        /// 插件被关闭
        /// </summary>
        public abstract Task OnDisable();

        public override string ToString()
        {
            return $"{Manifest.Name}({Manifest.Version})";
        }
    }
}
