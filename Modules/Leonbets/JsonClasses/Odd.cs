using System.Collections.Generic;
using JsonClasses;

namespace Leonbets.JsonClasses
{
    public class Market
    {
        public string id { get; set; }

        public bool open { get; set; }

        public string name { get; set; }

        public string family { get; set; }

        public List<Runner> runners { get; set; }
    }

}