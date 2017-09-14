using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BetFairApi
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderStatus
    {
        EXECUTION_COMPLETE,
        EXECUTABLE
    }
}
