using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public class CommandRegisteredException : CommandException
    {
        public CommandRegisteredException()
        {
        }

        public CommandRegisteredException(string command) : base(command)
        {
        }

        public CommandRegisteredException(string command, string message) : base(command, message)
        {
        }

        public CommandRegisteredException(string command, string message, Exception innerException) : base(command, message, innerException)
        {
        }
    }
}
