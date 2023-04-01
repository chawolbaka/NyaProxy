using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Command
{
    public class Option
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public int MinimumArgs { get; set; }

        internal Action<Command, CommandOptionEventArgs> Handler { get; set; }

        internal Func<Command, CommandOptionEventArgs, Task> AsyncHandler { get; set; }

        internal HashSet<string> Aliases;

        public Option(string name, Action<Command, CommandOptionEventArgs> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
        }

        public Option(string name, Action<Command, CommandOptionEventArgs> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, int minimumArgs, Action<Command, CommandOptionEventArgs> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumArgs = minimumArgs;
            Handler = handler;
        }

        public Option(string name, int minimumArgs, Action<Command, CommandOptionEventArgs> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumArgs = minimumArgs;
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, Action<Command, CommandOptionEventArgs> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Handler = handler;
        }

        public Option(string name, string description, Action<Command, CommandOptionEventArgs> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, int minimumArgs, Action<Command, CommandOptionEventArgs> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinimumArgs = minimumArgs;
            Handler = handler;
        }

        public Option(string name, string description, int minimumArgs, Action<Command, CommandOptionEventArgs> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinimumArgs = minimumArgs;
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }




        public Option(string name, Func<Command, CommandOptionEventArgs, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AsyncHandler = handler;
        }

        public Option(string name, Func<Command, CommandOptionEventArgs, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, int minimumArgs, Func<Command, CommandOptionEventArgs, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumArgs = minimumArgs;
            AsyncHandler = handler;
        }

        public Option(string name, int minimumArgs, Func<Command, CommandOptionEventArgs, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumArgs = minimumArgs;
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, Func<Command, CommandOptionEventArgs, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            AsyncHandler = handler;
        }

        public Option(string name, string description, Func<Command, CommandOptionEventArgs, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, int minimumArgs, Func<Command, CommandOptionEventArgs, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinimumArgs = minimumArgs;
            AsyncHandler = handler;
        }

        public Option(string name, string description, int minimumArgs, Func<Command, CommandOptionEventArgs, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinimumArgs = minimumArgs;
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }
    }
}
