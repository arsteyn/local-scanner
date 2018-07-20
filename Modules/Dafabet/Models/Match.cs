using System.Collections.Generic;

namespace Dafabet.Models
{
    internal class Match
    {
        public string HomeName { get; set; }

        public string AwayName { get; set; }

        public long MatchId { get; set; }

        public bool IsLive { get; set; }

        public MoreInfo MoreInfo { get; set; }

        public List<OddSet> oddset { get; set; }
    }
}