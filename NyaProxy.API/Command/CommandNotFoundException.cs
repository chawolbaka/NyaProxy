using System;

namespace NyaProxy.API.Command
{
    public class CommandNotFoundException : CommandException
    {

        public CommandNotFoundException()
        {
        }

        public CommandNotFoundException(string command) : base(command)
        {
        }

        public CommandNotFoundException(string command, string message) : base(command, message)
        {
        }

        public CommandNotFoundException(string command, string message, Exception innerException) : base(command, message, innerException)
        {
        }
    }
}
