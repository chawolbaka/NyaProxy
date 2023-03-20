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

        public string Value { get; set; }

        internal Action<Command, Option, ICommandHelper> Handler { get; set; }

        internal Func<Command, Option, ICommandHelper, Task> AsyncHandler { get; set; }

        internal HashSet<string> Aliases;


        public Option(string name, Action<Command, Option, ICommandHelper> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
        }

        public Option(string name, Action<Command, Option, ICommandHelper> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, Action<Command, Option, ICommandHelper> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Handler = handler;
        }

        public Option(string name, string description, Action<Command, Option, ICommandHelper> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, Func<Command, Option, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AsyncHandler = handler;
        }

        public Option(string name, Func<Command, Option, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Option(string name, string description, Func<Command, Option, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            AsyncHandler = handler;
        }

        public Option(string name, string description, Func<Command, Option, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            AsyncHandler = handler;
            Aliases = new HashSet<string>(aliases);
        }

    }

}
