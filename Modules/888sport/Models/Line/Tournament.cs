using System.Collections.Generic;
using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class Tournament
    {
        [JsonProperty(PropertyName = "tournament_id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "tournament_name")]
        public string TournamentName { get; set; }

        [JsonProperty(PropertyName = "events")]
        public List<Market> Games { get; set; } 
    }
}
