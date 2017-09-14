using System.Collections.Generic;
using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class Sport
    {
        [JsonProperty(PropertyName = "sport_id")]
        public int Id { get; set; }
        [JsonProperty(PropertyName = "sport_name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "tournaments")]
        public List<Tournament> Tournaments { get; set; } 
    }
}
