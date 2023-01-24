using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NyaProxy.API.Channle;

namespace NyaProxy
{
    public class ChannleContainer : IChannleContainer
    {
        public Dictionary<string, IChannle> RegisteredChannles { get; set; }

        public int Count => RegisteredChannles.Count;

        public IChannle this[string channleName] => RegisteredChannles[channleName];


        public ChannleContainer()
        {
            RegisteredChannles = new Dictionary<string, IChannle>();
        }

        public bool ContainsKey(string channleName)
        {
            return RegisteredChannles.ContainsKey(channleName);
        }

        public void Add(IChannle channle)
        {
            RegisteredChannles.Add(channle.Name, channle);
        }

        public void Remove(IChannle channle)
        {
            if (RegisteredChannles.ContainsKey(channle.Name))
                RegisteredChannles.Remove(channle.Name);
        }

        public void Remove(string channleName)
        {
            if (RegisteredChannles.ContainsKey(channleName))
                RegisteredChannles.Remove(channleName);
        }

        public IEnumerator<IChannle> GetEnumerator()
        {
            return RegisteredChannles.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return RegisteredChannles.Values.GetEnumerator();
        }
    }
}
