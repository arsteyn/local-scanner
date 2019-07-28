using System.Collections.Generic;
using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class BetOffer
    {

        [JsonProperty(PropertyName = "id")]
        public long id { get; set; }

        [JsonProperty(PropertyName = "eventId")]
        public long eventId { get; set; }
        public bool suspended { get; set; }

        [JsonProperty(PropertyName = "criterion")]
        public Criterion criterion { get; set; }

        [JsonProperty(PropertyName = "outcomes")]
        public List<Outcome> outcomes { get; set; }

        [JsonProperty(PropertyName = "betOfferType")]
        public BetOfferType betOfferType { get; set; }
    }

    public class BetOfferType
    {
        [JsonProperty(PropertyName = "id")]
        public long id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; set; }

        [JsonProperty(PropertyName = "englishName")]
        public string englishName { get; set; }
    }

    public class Criterion
    {
        [JsonProperty(PropertyName = "id")]
        public long id { get; set; }

        [JsonProperty(PropertyName = "label")]
        public string label { get; set; }


        [JsonProperty(PropertyName = "englishLabel")]
        public string englishLabel { get; set; }
    }
}