using System;
using Newtonsoft.Json;

namespace FonBet.SerializedClasses
{
    [Serializable]
    public class ParamBet : Bet
    {
        public ParamBet(CustomFactor factor)
            : base(factor)
        {
            this.Param = factor.Param;
        }

        [JsonProperty("param")]
        public int Param { get; set; }
    }
}