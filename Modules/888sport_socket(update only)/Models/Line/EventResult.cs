using System.Collections.Generic;
using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class EventSub
    {
        [JsonProperty(PropertyName = "event")]
        public Event Event { get; set; }

        [JsonProperty(PropertyName = "liveData")]
        public LiveData LiveData { get; set; }
    }

    public class LiveData
    {
        [JsonProperty(PropertyName = "eventId")]
        public long eventId { get; set; }

        [JsonProperty(PropertyName = "score")]
        public Score score { get; set; }
    }

    public class Score
    {
        [JsonProperty(PropertyName = "home")]
        public string home { get; set; }

        [JsonProperty(PropertyName = "away")]
        public string away { get; set; }
    }

    public class EventResult
    {
        [JsonProperty(PropertyName = "events")]
        public List<EventSub> Events { get; set; }

        
    }

    public class EventFull
    {
        [JsonProperty(PropertyName = "events")]
        public List<Event> Event { get; set; }

        [JsonProperty(PropertyName = "betOffers")]
        public List<BetOffer> BetOffers { get; set; }
    }
}
