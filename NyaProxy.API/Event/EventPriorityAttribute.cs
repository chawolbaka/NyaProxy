using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NyaProxy.API.Event
{
    [AttributeUsage(AttributeTargets.Method)]
    public class EventPriorityAttribute : Attribute
    {
        public EventPriority Priority { get; set; }

        public EventPriorityAttribute(EventPriority priority)
        {
            Priority = priority;
        }
        
        public static EventPriority GetPriority(Delegate @delegate)
        {
            var methodInfo = @delegate.Method;
            EventPriorityAttribute attribute = methodInfo.GetCustomAttribute(typeof(EventPriorityAttribute)) as EventPriorityAttribute;
            if (attribute != null)
                return attribute.Priority;
            else
                return EventPriority.Normal;
        }
    }
}
