using Microsoft.Extensions.Logging;
using NyaProxy.API;
using NyaProxy.API.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.CLI
{
    public class CommandHelper : ICommandHelper
    {
        public ILogger Logger => NyaProxy.Logger;
        public bool BlockRemainingCommands { get; set; }

    }
}
