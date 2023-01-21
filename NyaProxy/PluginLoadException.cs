using System;
using System.Runtime.Serialization;

namespace NyaProxy
{
    [Serializable]
    internal class PluginLoadException : Exception
    {
        public PluginLoadException()
        {
        }

        public PluginLoadException(string message) : base(message)
        {
        }

        public PluginLoadException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PluginLoadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}