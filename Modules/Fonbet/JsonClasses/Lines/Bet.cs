using System;
using Newtonsoft.Json;

namespace FonBet.SerializedClasses
{
    [Serializable]
    public class Bet
    {
        public Bet(CustomFactor factor)
        {
            this.@Event = factor.EventId;
            this.Factor = factor.FactorId;
            this.Value = (double)factor.CoeffValue;
        }

        [JsonProperty("num")]
        public int Num = 1;

        [JsonProperty("event")]
        public int Event { get; set; }

        [JsonProperty("factor")]
        public int Factor { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("score")]
        public string Score { get; set; }
    }
}