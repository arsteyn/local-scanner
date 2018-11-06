using System.Collections.Generic;
using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class Event
    {
        [JsonProperty(PropertyName = "sport_id")]
        public int sport_id { get; set; }

        [JsonProperty(PropertyName = "sport_name")]
        public string sport_name { get; set; }

        [JsonProperty(PropertyName = "tournament_name")]
        public string tournament_name { get; set; }

        [JsonProperty(PropertyName = "event_id")]
        public long event_id { get; set; }

        [JsonProperty(PropertyName = "event_name")]
        public string event_name { get; set; }

        [JsonProperty(PropertyName = "event_status_type")]
        public string event_status_type { get; set; }

        [JsonProperty(PropertyName = "event_result_total")]
        public string event_result_total { get; set; }

        [JsonProperty(PropertyName = "event_result_name")]
        public string event_result_name { get; set; }

    }
}
