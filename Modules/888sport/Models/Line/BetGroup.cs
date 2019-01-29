using System.Collections.Generic;
using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class BetGroup
    {
        [JsonProperty(PropertyName = "market_name")]
        public string Name { get; set; }
        
        [JsonProperty(PropertyName = "markets")]
        public List<Bet> Bets { get; set; }
    }
}
