using System;

namespace NyaProxy.API.Command
{
    public class CommandOptionEventArgs : EventArgs
    {
        public Option Option { get; set; }
        public ReadOnlyMemory<string> Arguments { get; set; }
        public ICommandHelper Helper { get; set; }

        public CommandOptionEventArgs(Option option, ICommandHelper helper)
        {
            Option = option ?? throw new ArgumentNullException(nameof(option));
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        public CommandOptionEventArgs(Option option, ReadOnlyMemory<string> arguments, ICommandHelper helper)
        {
            Option = option ?? throw new ArgumentNullException(nameof(option));
            Arguments = arguments;
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

    }
}
