using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Command
{
    public interface ICommandHelper
    {
        public INyaLogger Logger { get; }
        public bool BlockRemainingCommands { get; set; }
    }
}
