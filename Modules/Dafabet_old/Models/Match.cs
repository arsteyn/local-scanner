using System.Collections.Generic;

namespace Dafabet.Models
{
    public class MatchDataResult
    {
        public MatchDataResult()
        {
            this.leagues = new List<League>();
        }

        public List<League> leagues { get; set; }
    }

    public class Match
    {
        public Match()
        {
            this.oddset = new List<OddSet>();
        }

        public string HomeName { get; set; }

        public string AwayName { get; set; }

        public long MatchId { get; set; }

        public bool IsLive { get; set; }

        public MoreInfo MoreInfo { get; set; }

        public List<OddSet> oddset { get; set; }
    }
}