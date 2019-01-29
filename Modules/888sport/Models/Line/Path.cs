using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class Path
    {
        [JsonProperty(PropertyName = "id")]
        public long id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; set; }

        [JsonProperty(PropertyName = "englishName")]
        public string englishName { get; set; }

        [JsonProperty(PropertyName = "termKey")]
        public string termKey { get; set; }
    }
}