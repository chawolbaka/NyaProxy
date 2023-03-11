using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API
{
    public interface ICommandContainer
    {
        void Register(Command command);
        void Unregister(string commandName);
    }
}
