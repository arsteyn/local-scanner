using System.Collections.Generic;
using Newtonsoft.Json;

namespace BetFairApi
{
    public class PriceProjection
    {
        [JsonProperty(PropertyName = "priceData")]
        public IList<PriceData> PriceData { get; set; }

        [JsonProperty(PropertyName = "exBestOffersOverrides")]
        public ExBestOffersOverrides ExBestOffersOverrides { get; set; }
        
        [JsonProperty(PropertyName = "virtualise")]
        public bool? Virtualise { get; set; }

        [JsonProperty(PropertyName = "rolloverStakes")]
        public bool? RolloverStakes { get; set; }

        
    }
}
