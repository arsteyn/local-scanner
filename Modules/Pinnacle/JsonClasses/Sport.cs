using System.Collections.Generic;

namespace Pinnacle.JsonClasses
{
    public class Sport
    {
        public int SportId { get; set; }

        public string SportName { get; set; }

        public List<Market> Markets { get; set; }
        public List<Period> Periods { get; set; }
    }
}
