using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NyaProxy.API.Command
{
    public class CommandException : Exception
    {
        public string CommandName { get; set; }

        public CommandException() : base() { }
        public CommandException(string commandName) : base() { CommandName = commandName; }
        public CommandException(string commandName, string message) : base(message) { CommandName = commandName; }
        public CommandException(string commandName, string message, Exception innerException) : base(message, innerException) { CommandName = commandName; }
    }
}
