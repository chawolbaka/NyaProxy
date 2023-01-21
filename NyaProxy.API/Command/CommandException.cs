using System;
using System.Collections.Generic;
using System.Text;

namespace NyaProxy.API
{
    public class CommandException : Exception
    {
        public CommandException() : base() { }
        public CommandException(string message) : base(message) { }
        public CommandException(string message, Exception innerException) : base(message, innerException) { }
    }
}
