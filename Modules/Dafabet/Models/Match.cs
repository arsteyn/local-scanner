using System.Collections.Generic;

namespace Dafabet.Models
{
    public class Match
    {
        public Match()
        {
            this.oddset = new Dictionary<long, OddSet>();
        }

        public string leagueid { get; set; }
        public string leaguenameen { get; set; }
        public int sporttype { get; set; }
        public string eventstatus { get; set; }
        public long homeid { get; set; }
        public string hteamnameen { get; set; }
        public long awayid { get; set; }
        public string ateamnameen { get; set; }
        public long matchid { get; set; }
        public int livehomescore { get; set; }
        public int liveawayscore { get; set; }
        public int mc { get; set; }

        public Dictionary<long, OddSet> oddset { get; set; }

    }
}