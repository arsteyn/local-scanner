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
        public long Id { get; set; }

        public string name { get; set; }

        public List<Competitor> competitors { get; set; }

        public bool open { get; set; }

        public long kickoff { get; set; }

        public string lastUpdated { get; set; }

        public League league { get; set; }

        public LiveStatus liveStatus { get; set; }

        public List<Market> markets { get; set; }
    }

    public class LiveStatus
    {
        public string stage { get; set; }
        public string score { get; set; }
        public string setScores { get; set; }

    }

    public class League
    {
        public long Id { get; set; }
        public string name { get; set; }
        public Sport sport { get; set; }
        public int weight { get; set; }

    }

    public class Sport
    {
        public long Id { get; set; }
        public string name { get; set; }
        public BetLine betline { get; set; }

        public int weight { get; set; }

        public string family { get; set; }




    }

    public class BetLine
    {
        public string name { get; set; }
        public string combination { get; set; }
    }

    public class Competitor
    {
        public long Id { get; set; }
        public string name { get; set; }
        public string homeAway { get; set; }
        public string type { get; set; }
        public string logo { get; set; }

    }
}