using System;

namespace NyaProxy.API.Command
{
    public class CommandLeastRequiredException : CommandException
    {
        public Command Command { get; set; }
        public int MinimumArgs { get; set; }

        public CommandLeastRequiredException(Command command) : base(command.Name) { }
        public CommandLeastRequiredException(Command command, int minimumArgs) : base(command.Name)
        {
            Command = command;
            MinimumArgs = minimumArgs;
        }
        public CommandLeastRequiredException(Command command, int minimumArgs, string message) : base(command.Name, message)
        {
            Command = command;
            MinimumArgs = minimumArgs;
        }
        public CommandLeastRequiredException(Command command, int minimumArgs, string message, Exception innerException) : base(command.Name, message, innerException)
        {
            Command = command;
            MinimumArgs = minimumArgs;
        }
    }
}
