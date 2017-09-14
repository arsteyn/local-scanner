using System.Collections.Generic;
using JsonClasses;

namespace Leonbets.JsonClasses
{


    public class Odd
    {
        public string id { get; set; }

        public int? oddsType { get; set; }

        public string name { get; set; }

        public string specialOddsValue { get; set; }

        public List<Runner> runners { get; set; }
    }

}