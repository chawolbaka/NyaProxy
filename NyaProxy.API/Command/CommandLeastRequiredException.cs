using System;

namespace NyaProxy.API
{
    public class CommandLeastRequiredException : CommandException
    {
        public int MinimumArgs { get; set; }

        public CommandLeastRequiredException(string command) : base(command) { }
        public CommandLeastRequiredException(string command, int minimumArgs) : base(command)
        {
            MinimumArgs = minimumArgs;
        }
        public CommandLeastRequiredException(string command, int minimumArgs, string message) : base(command, message)
        {
            MinimumArgs = minimumArgs;
        }
        public CommandLeastRequiredException(string command, int minimumArgs, string message, Exception innerException) : base(command, message, innerException)
        {
            MinimumArgs = minimumArgs;
        }
    }
}
