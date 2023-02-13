using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy
{
    public class DisconnectException : Exception
    {
        public DisconnectException()
        {
        }

        public DisconnectException(string message) : base(message)
        {
        }

        public DisconnectException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DisconnectException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
