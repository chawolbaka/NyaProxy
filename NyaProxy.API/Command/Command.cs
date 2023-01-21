using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace NyaProxy.API
{
    public abstract class Command
    {
        public abstract string Name { get; }
        public abstract string Usage { get; }
        public abstract string Description { get; }
        public abstract void Execute(ReadOnlySpan<string> args);
        public abstract IEnumerable<string> GetTabCompletions(ReadOnlySpan<string> args);
        public virtual int MinimumArgs => 0;
        public virtual string Helper => Usage;
    }
}
