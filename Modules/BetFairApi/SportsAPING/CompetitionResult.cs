using System.Text;
using Newtonsoft.Json;

namespace BetFairApi
{
    public class CompetitionResult
    {
        [JsonProperty(PropertyName = "competition")]
        public Competition Competition { get; set; }

        [JsonProperty(PropertyName = "marketCount")]
        public int MarketCount { get; set; }

        public override string ToString()
        {
            return new StringBuilder().AppendFormat("{0}", "CompetitionResult")
                        .AppendFormat(" : {0}", Competition)
                        .AppendFormat(" : MarketCount={0}", MarketCount)
                        .ToString();
        }
    }
}
