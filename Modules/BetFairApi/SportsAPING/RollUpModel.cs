using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BetFairApi
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RollUpModel
    {
        STAKE, PAYOUT, MANAGED_LIABILITY, NONE
    }
}
