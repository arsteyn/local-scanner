using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class GameId
    {
        [JsonProperty(PropertyName = "event_id")]
        public int Id { get; set; }
    }
}
