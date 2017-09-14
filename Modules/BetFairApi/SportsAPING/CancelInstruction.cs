using System.Text;
using Newtonsoft.Json;

namespace BetFairApi
{
    public class CancelInstruction
    {
        [JsonProperty(PropertyName = "betId")]
        public string BetId { get; set; }

        [JsonProperty(PropertyName = "sizeReduction")]
        public double? SizeReduction { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("{0}", "CancelInstruction")
                        .AppendFormat(" : BetId={0}", BetId)
                        .AppendFormat(" : SizeReduction={0}", SizeReduction);

            return sb.ToString();
        }
    }
}
