using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace BetFairApi
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderType
    {
        LIMIT,				// normal exchange limit order for immediate execution
        LIMIT_ON_CLOSE,		// limit order for the auction (SP)
        MARKET_ON_CLOSE		// market order for the auction (SP)
    }
}
