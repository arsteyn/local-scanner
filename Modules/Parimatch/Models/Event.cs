using System;
using System.Collections.Generic;

namespace Parimatch.Models
{
    public class Event
    {
        public Event()
        {
            BetBlocks = new List<BetBlock>();
        }

        public string[] HeaderLine { get; set; }
        public string Sport { get; set; }

        public string Home { get; set; }
        public string Away { get; set; }

        public DateTime Date { get; set; }

        public string Score1 { get; set; }
        public string Score2 { get; set; }

        public List<BetBlock> BetBlocks { get; set; }
        public string DefaultType { get; internal set; }
        public string[] PeriodScores { get; internal set; }
    }
}