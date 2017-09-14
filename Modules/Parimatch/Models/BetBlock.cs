using System.Collections.Generic;

namespace Parimatch.Models
{
    public class BetBlock
    {
        public BetBlock(string name, Event @event)
        {
            Name = name;
            Event = @event;
            Outcomes = new List<Outcome>();
        }

        public Event Event { get; set; }
        public string Name { get; set; }
        public List<Outcome> Outcomes { get; private set; }
        public string PriodName { get; set; }
        public void AddOutcome(Outcome outcome)
        {
            outcome.BetBlock = this;
            outcome.Event = this.Event;
            Outcomes.Add(outcome);
        }
    }
}