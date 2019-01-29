using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class Outcome
    {
        [JsonProperty(PropertyName = "id")]
        public long id { get; set; }

        [JsonProperty(PropertyName = "label")]
        public string label { get; set; }

        [JsonProperty(PropertyName = "englishLabel")]
        public string englishLabel { get; set; }


        [JsonProperty(PropertyName = "line")]
        public decimal? line { get; set; }

        [JsonProperty(PropertyName = "odds")]
        public int odds { get; set; }

        [JsonProperty(PropertyName = "participant")]
        public string participant { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string type { get; set; }

        [JsonProperty(PropertyName = "betOfferId")]
        public long betOfferId { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string status { get; set; }
    }
}