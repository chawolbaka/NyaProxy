using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NyaProxy.API.Command
{
    public class CommandException : Exception
    {
        public string Command { get; set; }

        public CommandException() : base() { }
        public CommandException(string command) : base() { Command = command; }
        public CommandException(string command, string message) : base(message) { Command = command; }
        public CommandException(string command, string message, Exception innerException) : base(message, innerException) { Command = command; }
    }
}
