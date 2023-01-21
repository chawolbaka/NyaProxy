using MinecraftProtocol.Utils;

namespace NyaProxy
{
    public class CancelEventArgs: ICancelEvent
    {
        protected bool _isCancelled;
        public virtual bool IsCancelled => _isCancelled;

        /// <summary>
        /// 阻止事件被继续执行下去
        /// </summary>
        public virtual void Cancel()
        {
            _isCancelled = true;
        }
    }
}