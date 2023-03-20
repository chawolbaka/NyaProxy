using System;
using System.Collections.Generic;
using System.Text;

namespace NyaProxy.API.Command
{
    public class CommandFormatException : CommandException
    {
        public Command Command { get; set; }
        public string ParamName { get; }

        public CommandFormatException(Command command) : base(command.Name)
        {
            Command = command;
        }
        public CommandFormatException(Command command, string paramName) : base(command.Name)
        {
            Command = command;
            ParamName = paramName;
        }
        public CommandFormatException(Command command, string paramName, string message) : base(command.Name, message)
        {
            Command = command;
            ParamName = paramName;
        }
        public CommandFormatException(Command command, string paramName, string message, Exception innerException) : base(command.Name, message, innerException)
        {
            Command = command;
            ParamName = paramName;
        }
    }
}
