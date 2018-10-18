using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class Outcome
    {

        [JsonProperty(PropertyName = "outcome_coef")]
        public decimal outcome_coef { get; set; }

        [JsonProperty(PropertyName = "outcome_id")]
        public long outcome_id { get; set; }


        [JsonProperty(PropertyName = "outcome_name")]
        public string outcome_name { get; set; }

        [JsonProperty(PropertyName = "outcome_param")]
        public string outcome_param { get; set; }

        [JsonProperty(PropertyName = "outcome_perc_stat")]
        public decimal outcome_perc_stat { get; set; }


        [JsonProperty(PropertyName = "outcome_short_name")]
        public string outcome_short_name { get; set; }

        [JsonProperty(PropertyName = "outcome_tag")]
        public string outcome_tag { get; set; }

        [JsonProperty(PropertyName = "outcome_visible")]
        public bool outcome_visible { get; set; }

        [JsonProperty(PropertyName = "participant_number")]
        public string participant_number { get; set; }
    }
}