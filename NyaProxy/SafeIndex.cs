using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NyaProxy
{
    public class SafeIndex
    {
        public readonly int Max;

        public SafeIndex(int max)
        {
            Max = max;
        }

        private int LastIndex;
        private SpinLock IndexLock = new SpinLock();
        public int Get()
        {
            int result = 0; 
            bool lockTaken = false;
            try
            {
                IndexLock.Enter(ref lockTaken);
                if (LastIndex + 1 >= Max)
                    LastIndex = 0;
                else
                    result = ++LastIndex;
            }
            finally
            {
                if (lockTaken)
                    IndexLock.Exit();
            }

            return result;
        }
    }
}
