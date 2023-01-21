using System;
using System.Collections.Generic;
using System.Text;

namespace NyaProxy.API
{
    public class CommandFormatException : CommandException
    {
        public virtual string ParamName { get; }

        public CommandFormatException() : base() { }
        public CommandFormatException(string message) : base(message) { }
        public CommandFormatException(string message, Exception innerException) : base(message, innerException) { }

        public CommandFormatException(string message, string paramName) : base(message) { ParamName = paramName; }
        public CommandFormatException(string message, string paramName, Exception innerException) : base(message, innerException) { paramName = ParamName; }
    }
}
