using System.Collections.Generic;
using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class BetGroup
    {
        [JsonProperty(PropertyName = "market_name")]
        public string Name { get; set; }
        
        [JsonProperty(PropertyName = "markets")]
        public List<Bet> Bets { get; set; }
    }
}
