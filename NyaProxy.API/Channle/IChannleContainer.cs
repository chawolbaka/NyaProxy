using System.Collections.Generic;


namespace NyaProxy.API.Channle
{
    public interface IChannleContainer : IEnumerable<IChannle>
    {
        int Count { get; }
        IChannle this[string channleName] { get; }

        bool ContainsKey(string channleName);

        void Add(IChannle channle);

        void Remove(IChannle channle); 
        void Remove(string channleName);
        
    }
}
