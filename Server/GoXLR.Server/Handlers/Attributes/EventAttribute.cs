using System;

namespace GoXLR.Server.Handlers.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class EventAttribute : Attribute
    {
        public string Event { get; }

        public EventAttribute(string eventName)
        {
            Event = eventName;
        }
    }
}
