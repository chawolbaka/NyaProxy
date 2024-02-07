using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Command
{
    public interface ICommandHelper
    {
        public ILogger Logger { get; }
        public bool BlockRemainingCommands { get; set; }
    }
}
