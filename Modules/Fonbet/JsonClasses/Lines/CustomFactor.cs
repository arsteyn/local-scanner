using Newtonsoft.Json;

namespace FonBet.SerializedClasses
{
    public class CustomFactor
    {
        [JsonProperty("e")]
        public int EventId { get; set; }

        [JsonProperty("f")]
        public int FactorId { get; set; }

        [JsonProperty("v")]
        public decimal CoeffValue { get; set; }

        [JsonProperty("p")]
        public int Param { get; set; }

        [JsonProperty("pt")]
        public string CoeffParam { get; set; }
        public bool isLive { get; set; }
        public int? lo { get; set; }
        public int? hi { get; set; }
    }
}