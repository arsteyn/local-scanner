using System.Collections.Generic;
using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class EventsShort
    {
        [JsonProperty(PropertyName = "events")]
        public List<Event> Events { get; set; }
    }

  
}
