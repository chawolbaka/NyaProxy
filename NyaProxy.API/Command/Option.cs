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
        
        internal HashSet<string> Aliases;
        
        public Func<Option, ICommandHelper, Task> Handler { get; set; }

        public string Value { get; set; }

        public Option(string name, Func<Option, ICommandHelper, Task> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
        }

        public Option(string name, Func<Option, ICommandHelper, Task> handler, params string[] aliases)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Handler = handler;
            Aliases = new HashSet<string>(aliases);
        }
    }

}
