using System;
using Newtonsoft.Json;

namespace BetFairApi
{
    public class ExBestOffersOverrides
    {
        [JsonProperty(PropertyName = "bestPricesDepth")]
        public int BestPricesDepth { get; set; }

        [JsonProperty(PropertyName = "rollupModel")]
        public RollUpModel RollUpModel { get; set; }

        [JsonProperty(PropertyName = "rollupLimit")]
        public int RollUpLimit { get; set; }

        [JsonProperty(PropertyName = "rollupLiabilityThreshold")]
        public Double RollUpLiabilityThreshold { get; set; }

        [JsonProperty(PropertyName = "rollupLiabilityFactor")]
        public int RollUpLiabilityFactor { get; set; }
    }
}
