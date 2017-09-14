using System.Collections.Generic;

namespace Leonbets.JsonClasses
{
    public class EventsList
    {
        public bool enabled { get; set; }

        public List<Event> events { get; set; }
    }

    public class Event
    {
        public string score { get; set; }

        public string lName { get; set; }

        public int oddsCount { get; set; }

        public long Id { get; set; }

        public string name { get; set; }

        public long kickoffDate { get; set; }

        public string sName { get; set; }

        public string eventStatus { get; set; }

        public List<Odd> markets { get; set; }
    }
}