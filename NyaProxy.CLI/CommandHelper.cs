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
        public INyaLogger Logger { get; }
        public bool BlockRemainingCommands { get; set; }

        
        public CommandHelper(INyaLogger logger)
        {
            Logger = logger;
        }
    }
}
