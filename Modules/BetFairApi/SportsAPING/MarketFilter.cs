using System.Collections.Generic;
using Newtonsoft.Json;

namespace BetFairApi
{
    public class MarketFilter
    {
        [JsonProperty(PropertyName = "textQuery")]
        public string TextQuery { get; set; }

        [JsonProperty(PropertyName = "exchangeIds")]
        public IList<string> ExchangeIds { get; set; }

        [JsonProperty(PropertyName = "eventTypeIds")]
        public IList<string> EventTypeIds { get; set; }

        [JsonProperty(PropertyName = "eventIds")]
        public IList<string> EventIds { get; set; }

        [JsonProperty(PropertyName = "competitionIds")]
        public IList<string> CompetitionIds { get; set; }

        [JsonProperty(PropertyName = "marketIds")]
        public IList<string> MarketIds { get; set; }

        [JsonProperty(PropertyName = "venues")]
        public IList<string> Venues { get; set; }

        [JsonProperty(PropertyName = "bspOnly")]
        public bool? BspOnly { get; set; }

        [JsonProperty(PropertyName = "turnInPlayEnabled")]
        public bool? TurnInPlayEnabled { get; set; }

        [JsonProperty(PropertyName = "inPlayOnly")]
        public bool? InPlayOnly { get; set; }

        [JsonProperty(PropertyName = "marketBettingTypes")]
        public IList<MarketBettingType> MarketBettingTypes { get; set; }

        [JsonProperty(PropertyName = "marketCountries")]
        public IList<string> MarketCountries { get; set; }

        [JsonProperty(PropertyName = "marketTypeCodes")]
        public IList<string> MarketTypeCodes { get; set; }

        [JsonProperty(PropertyName = "marketStartTime")]
        public TimeRange MarketStartTime { get; set; }

        [JsonProperty(PropertyName = "withOrders")]
        public IList<OrderStatus> WithOrders { get; set; }


         
    }
}
