using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class Timer
    {
        [JsonProperty(PropertyName = "action")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "timer")]
        public long? Value { get; set; }

        [JsonProperty(PropertyName = "timer2")]
        public long? Value2 { get; set; }

        [JsonProperty(PropertyName = "timer_vector")] // "asc"
        public string Vector { get; set; }
    }
}
