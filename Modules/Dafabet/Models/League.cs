using System.Collections.Generic;

namespace Dafabet.Models
{
    internal class League
    {
        public int SportType { get; set; }

        public string SportName { get; set; }

        public string LeagueName { get; set; }

        public List<Match> matches { get; set; }


    }
}