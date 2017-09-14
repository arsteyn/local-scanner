using System.Collections.Generic;

namespace Bwin.JsonClasses
{

    public class Event
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Self { get; set; }

        public string Player1 { get; set; }

        public string Player2 { get; set; }

        public bool IsPreMatch { get; set; }

        public Sport Sport { get; set; }
        public League League { get; set; }
        public List<Market> Markets { get; set; }
        public Scoreboard Scoreboard { get; set; }
    }
}
