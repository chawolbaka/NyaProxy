using System;

namespace NyaProxy.API.Command
{
    public class MissingArgumentException : CommandException
    {
        public string Argument { get; set; }

        public MissingArgumentException(string command, string argument) : base(command)
        {
            Argument = argument;
        }
        public MissingArgumentException(string command, string argument, string message) : base(command, message)
        {
            Argument = argument;
        }
        public MissingArgumentException(string command, string argument, string message, Exception innerException) : base(command, message, innerException)
        {
            Argument = argument;
        }
    }
}
