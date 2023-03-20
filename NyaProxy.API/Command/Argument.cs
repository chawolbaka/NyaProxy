using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Command
{

    public class Argument
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<Command, Argument, ICommandHelper, Task> Handler { get; set; }

        internal HashSet<string> Aliases;


        public Argument(string name, Func<Command, Argument, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
        }

        public Argument(string name, string description, Func<Command, Argument, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Handler = handler;
        }

        public Argument(string name, Func<Command, Argument, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }

        public Argument(string name, string description, Func<Command, Argument, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }
    }
}
