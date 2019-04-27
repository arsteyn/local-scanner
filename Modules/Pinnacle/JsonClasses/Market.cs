using System.Collections.Generic;

namespace Pinnacle.JsonClasses
{
    public class Market
    {
        public string MarketName { get; set; }

        public bool HideMarket { get; set; }

        public List<string> HeaderLabels { get; set; }

        public Dictionary<string, GamesContainer> GamesContainers { get; set; }
    }
}
