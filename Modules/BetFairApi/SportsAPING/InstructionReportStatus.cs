using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BetFairApi
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum InstructionReportStatus
    {
        SUCCESS,
        FAILURE,
        TIMEOUT,
    }
}
