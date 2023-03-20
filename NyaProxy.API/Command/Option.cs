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

        public Func<Command, Option, ICommandHelper, Task> Handler { get; set; }

        internal HashSet<string> Aliases;

        public Option(string name, Func<Command, Option, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
        }

        public Option(string name, Func<Command, Option, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }
    }

}
