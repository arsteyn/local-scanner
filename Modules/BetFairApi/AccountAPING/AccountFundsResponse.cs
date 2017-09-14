using Newtonsoft.Json;

namespace BetFairApi
{
    public class AccountFundsResponse
    {
        [JsonProperty(PropertyName = "availableToBetBalance")]
        public decimal AvailableToBetBalance { get; set; }

        [JsonProperty(PropertyName = "exposure")]
        public decimal Exposure { get; set; }

        [JsonProperty(PropertyName = "retainedCommission")]
        public decimal RetainedCommission { get; set; }

        [JsonProperty(PropertyName = "exposureLimit")]
        public decimal ExposureLimit { get; set; }
    }
}
