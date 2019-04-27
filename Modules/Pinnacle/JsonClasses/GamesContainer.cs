using System.Collections.Generic;

namespace Pinnacle.JsonClasses
{
    public class GamesContainer
    {
        public string DisplayName { get; set; }

        public string Name { get; set; }

        public bool IsVisible { get; set; }

        public int LeagueId { get; set; }

        public List<GameLine> GameLines { get; set; }
    }
}