using System.Collections.Generic;

namespace Partypoker.JsonClasses
{
  
    public class FixturesResponse
    {
        public List<Fixture> fixtures { get; set; }

        public int totalCount { get; set; }

        public class Fixture
        {
            public long id { get; set; }
        }
    }
}