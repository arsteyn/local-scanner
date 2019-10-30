using Newtonsoft.Json;

namespace FonBet.SerializedClasses
{
    public class EventMisc
    {
        public int id { get; set; }
        public int liveDelay { get; set; }

        [JsonProperty("score1")]
        public int Score1 { get; set; }

        [JsonProperty("score2")]
        public int Score2 { get; set; }
        public string comment { get; set; }
        public int? timerDirection { get; set; }
        public int? timerSeconds { get; set; }
        public int? timerUpdateTimestamp { get; set; }
        public int? servingTeam { get; set; }
    }
}