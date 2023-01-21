using NyaProxy.API;
using System;

namespace NyaProxy
{
    public abstract class TransportEventArgs : CancelEventArgs, IBlockEventArgs
    {
        public virtual bool IsBlock => _isBlock;
        protected bool _isBlock;

        /// <summary>
        /// 将包拦截下来
        /// </summary>
        public virtual void Block()
        {
            _isBlock = true;
        }
    }
}