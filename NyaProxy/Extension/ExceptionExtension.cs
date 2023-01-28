using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.Extension
{
    public static class ExceptionExtension
    {
        public static bool CheckSocketException(this Exception e, out string message)
        {
            message = null;
            if (e is AggregateException ae)
            {
                foreach (var ex in ae.Flatten().InnerExceptions)
                {
                    if (e is SocketException)
                        message = ex.Message;
                }
            }
            else if (e is SocketException)
                message = e.Message;
            else if (e.InnerException != null && e.InnerException is SocketException)
                message = e.InnerException.Message;

            return !string.IsNullOrEmpty(message);
        }
    }
}
