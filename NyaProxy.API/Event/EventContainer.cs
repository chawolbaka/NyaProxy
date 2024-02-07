using System;
using System.Linq;
using System.Collections.Generic;
using MinecraftProtocol.Utils;
using Microsoft.Extensions.Logging;

namespace NyaProxy.API.Event
{
    public class EventContainer<TEventArgs>
    {
        public List<List<EventHandler<TEventArgs>>> Events { get; set; }

        public EventContainer()
        {
            Events = new();
            int max = ((int[])System.Enum.GetValues(typeof(EventPriority))).Max(x => x);
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

        public void Invoke(object sender, TEventArgs e, ILogger logger, bool ignoreException = true)
        {
            for (int y = 0; y < Events.Count; y++)
            {
                for (int x = 0; x < Events[y].Count; x++)
                {
                    EventHandler<TEventArgs> handler = Events[y][x];
                    if (handler == null)
                        continue;

                    try
                    {
                        if (e is ICancelEvent eventArgs)
                        {
                            handler.Invoke(sender, e);
                            if (eventArgs.IsCancelled)
                                return;
                        }
                        else
                        {
                            handler.Invoke(sender, e);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ignoreException)
                            logger.LogError(ex);
                        else
                            throw;
                    }
                }
            }
        }
    }
}
