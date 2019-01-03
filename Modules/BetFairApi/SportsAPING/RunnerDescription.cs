using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace BetFairApi
{
    public class RunnerDescription
    {
        private string _runnerName;

        [JsonProperty(PropertyName = "selectionId")]
        public long SelectionId { get; set; }

        [JsonProperty(PropertyName = "runnerName")]
        public string RunnerName
        {
            get => _runnerName.ToLower();
            set => _runnerName = value;
        }

        [JsonProperty(PropertyName = "handicap")]
        public double Handicap { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public Dictionary<string, string> Metadata { get; set; }

        public override string ToString()
        {
            return new StringBuilder().AppendFormat("{0}", "RunnerDescription")
                        .AppendFormat(" : SelectionId={0}", SelectionId)
                        .AppendFormat(" : runnerName={0}", RunnerName)
                        .AppendFormat(" : Handicap={0}", Handicap)
                        .AppendFormat(" : Metadata={0}", Metadata)
                        .ToString();
        }
    }
}
