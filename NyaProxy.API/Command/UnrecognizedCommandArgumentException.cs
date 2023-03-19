using System;

namespace NyaProxy.API.Command
{
    public class UnrecognizedArgumentException : CommandException
    {
        public string Argument { get; set; } 

        public UnrecognizedArgumentException(string command, string argument) : base(command)
        {
            Argument = argument;
        }
        public UnrecognizedArgumentException(string command, string argument, string message) : base(command, message)
        {
            Argument = argument;
        }
        public UnrecognizedArgumentException(string command, string argument, string message, Exception innerException) : base(command, message, innerException)
        {
            Argument = argument;
        }
    }
}
