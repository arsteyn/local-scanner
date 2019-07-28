using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class GameId
    {
        [JsonProperty(PropertyName = "event_id")]
        public int Id { get; set; }
    }
}
