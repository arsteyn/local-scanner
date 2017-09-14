using System.Collections.Generic;

namespace Bwin.JsonClasses
{
    public class EventsResponce
    {
        public List<Event> events { get; set; }
    }

    public class EventResponce
    {
        public Event @event { get; set; }
    }
}