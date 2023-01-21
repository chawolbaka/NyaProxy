using System;
using System.Collections.Generic;

namespace NyaProxy.API.Config
{
    public interface IManifest
    {
        /// <summary>
        /// 插件Id
        /// </summary>
        public string UniqueId { get; }

        /// <summary>
        /// 插件名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 插件作者
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// 插件简介
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// 插件至少需要什么版本的Api
        /// </summary>
        public Version MinimumApiVersion { get; }

    }
}
