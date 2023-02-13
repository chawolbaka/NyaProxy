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
        public static bool CheckException<T>(this Exception e, out string message) where T : Exception         
        {
            message = null;
            if (e is AggregateException ae)
            {
                foreach (var ex in ae.Flatten().InnerExceptions)
                {
                    if (e is T)
                        message = ex.Message;
                }
            }
            else if (e is T)
                message = e.Message;
            else if (e.InnerException != null && e.InnerException is T)
                message = e.InnerException.Message;

            return !string.IsNullOrEmpty(message);
        }
    }
}
