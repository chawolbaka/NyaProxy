using System;
using System.Linq;
using System.Collections.Generic;
using MinecraftProtocol.Utils;
using NyaProxy.API.Event;

namespace NyaProxy
{
    public class EventContainer<TEventArgs>
    {
       public List<List<EventHandler<TEventArgs>>> Events { get; set; }

        public EventContainer()
        {
            Events = new ();
            int max = ((int[])Enum.GetValues(typeof(EventPriority))).Max(x => x);
            for (int i = 0; i < max; i++)
            {
                Events.Add(new List<EventHandler<TEventArgs>>());
            }
        }

        public void Add(EventHandler<TEventArgs> eventHandler)
        {
            Events[(int)EventPriorityAttribute.GetPriority(eventHandler)].Add(eventHandler);
        }

        public void Remove(EventHandler<TEventArgs> eventHandler)
        {
            Events[(int)EventPriorityAttribute.GetPriority(eventHandler)].RemoveAll(x => x == eventHandler);
        }

        public void Invoke(object sender, TEventArgs e)
        {
            for (int i = 0; i < Events.Count; i++)
            {
                for (int x = 0; x < Events[i].Count; x++)
                {
                    EventHandler<TEventArgs> handler = Events[i][x];
                    if (e is ICancelEvent eventArgs)
                    {
                        handler?.Invoke(sender, e);
                        if (eventArgs.IsCancelled)
                            return;
                    }
                    else
                    {
                        handler?.Invoke(sender, e);
                    }
                }
            }
        }
    }
}
