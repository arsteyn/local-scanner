using System.Collections.Generic;

namespace Dafabet.Models
{
    public class League
    {

        public string SportName { get; set; }

        public string LeagueName { get; set; }

        public List<Match> matches { get; set; }
        public int GameId { get; set; }
    }
}