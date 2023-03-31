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

        internal Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> Handler { get; set; }

        internal Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> AsyncHandler { get; set; }

        internal HashSet<string> Aliases;

        public Option(string name, Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
        }

        public Option(string name, Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, int minimumArgs, Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumArgs = minimumArgs;
            Handler = handler;
        }

        public Option(string name, int minimumArgs, Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumArgs = minimumArgs;
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Handler = handler;
        }

        public Option(string name, string description, Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, int minimumArgs, Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinimumArgs = minimumArgs;
            Handler = handler;
        }

        public Option(string name, string description, int minimumArgs, Action<Command, Option, ReadOnlyMemory<string>, ICommandHelper> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinimumArgs = minimumArgs;
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }




        public Option(string name, Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AsyncHandler = handler;
        }

        public Option(string name, Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, int minimumArgs, Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumArgs = minimumArgs;
            AsyncHandler = handler;
        }

        public Option(string name, int minimumArgs, Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MinimumArgs = minimumArgs;
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            AsyncHandler = handler;
        }

        public Option(string name, string description, Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, int minimumArgs, Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinimumArgs = minimumArgs;
            AsyncHandler = handler;
        }

        public Option(string name, string description, int minimumArgs, Func<Command, Option, ReadOnlyMemory<string>, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            MinimumArgs = minimumArgs;
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }
    }
}
