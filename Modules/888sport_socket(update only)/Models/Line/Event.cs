using System.Collections.Generic;
using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class Event
    {
        [JsonProperty(PropertyName = "id")]
        public long id { get; set; }

        [JsonProperty(PropertyName = "englishName")]
        public string englishName { get; set; }

        [JsonProperty(PropertyName = "homeName")]
        public string homeName { get; set; }

        [JsonProperty(PropertyName = "awayName")]
        public string awayName { get; set; }

        [JsonProperty(PropertyName = "sport")]
        public string sport { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string state { get; set; }


        [JsonProperty(PropertyName = "path")]
        public List<Path> path { get; set; }
    }
}
