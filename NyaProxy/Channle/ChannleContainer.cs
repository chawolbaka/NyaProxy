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
        public Dictionary<string, IChannle> Channles { get; set; }

        public int Count => Channles.Count;

        public IChannle this[string channleName] => Channles[channleName];


        public ChannleContainer()
        {
            Channles = new Dictionary<string, IChannle>();
        }

        public bool ContainsKey(string channleName)
        {
            return Channles.ContainsKey(channleName);
        }

        public void Add(IChannle channle)
        {
            Channles.Add(channle.Name, channle);
        }

        public void Remove(IChannle channle)
        {
            if (Channles.ContainsKey(channle.Name))
                Channles.Remove(channle.Name);
        }

        public void Remove(string channleName)
        {
            if (Channles.ContainsKey(channleName))
                Channles.Remove(channleName);
        }

        public IEnumerator<IChannle> GetEnumerator()
        {
            return Channles.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Channles.Values.GetEnumerator();
        }
    }
}
