using System.Collections.Generic;
using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class Section
    {
        [JsonProperty(PropertyName = "result_type_id")]
        public int Id { get; set; }
        
        [JsonProperty(PropertyName = "result_type_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "market_groups")]
        public List<BetGroup> BetsGroup { get; set; }
    }
}
