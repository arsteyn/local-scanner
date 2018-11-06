using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class Odds
    {
        [JsonProperty(PropertyName = "outcome_id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "outcome_type_id")]
        public string OddsTypeId { get; set; }

        [JsonProperty(PropertyName = "outcome_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "outcome_coef")]
        public double? Value { get; set; }

        [JsonProperty(PropertyName = "outcome_visible")]
        public string IsHidden { get; set; }
    }
}
