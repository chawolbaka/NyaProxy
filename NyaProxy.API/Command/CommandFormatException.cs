using System;
using System.Collections.Generic;
using System.Text;

namespace NyaProxy.API.Command
{
    public class CommandFormatException : CommandException
    {
        public virtual string ParamName { get; }

        public CommandFormatException(string command) : base(command) { }
        public CommandFormatException(string command, string paramName) : base(command)
        {
            ParamName = paramName;
        }
        public CommandFormatException(string command, string paramName, string message) : base(command, message)
        {
            ParamName = paramName;
        }
        public CommandFormatException(string command, string paramName, string message, Exception innerException) : base(command, message, innerException)
        {
            ParamName = paramName;
        }
    }
}
