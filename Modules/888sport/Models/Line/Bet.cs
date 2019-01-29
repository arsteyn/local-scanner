using System.Collections.Generic;
using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class Bet
    {
        [JsonProperty(PropertyName = "market_id")]
        public string MarketId { get; set; }

        [JsonProperty(PropertyName = "market_suspend")]
        public string IsDisabled { get; set; }

        [JsonProperty(PropertyName = "outcomes")]
        public List<Odds> Odds { get; set; }
    }
}
