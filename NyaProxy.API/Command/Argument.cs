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

        public Func<Argument, ICommandHelper, Task> Handler { get; set; }

        internal HashSet<string> Aliases;

        public Argument(string name, Func<Argument, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
        }
        public Argument(string name, Func<Argument, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }
    }
}
