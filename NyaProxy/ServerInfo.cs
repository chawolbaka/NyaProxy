using NyaProxy.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy
{
    public class ServerInfo : IServer
    {

        public string Host { get; set; }

        public ServerInfo(string host)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }


    }
}