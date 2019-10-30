using Newtonsoft.Json;

namespace FonBet.SerializedClasses
{
    public class Event
    {
        public int id { get; set; }

        [JsonProperty("parentId")]
        public int? ParentId { get; set; }
        public string sortOrder { get; set; }
        public int level { get; set; }
        public int num { get; set; }

        [JsonProperty("sportId")]
        public int SportId { get; set; }
        public int kind { get; set; }
        public int rootKind { get; set; }
        public string name { get; set; }
        public string namePrefix { get; set; }
        public int startTime { get; set; }
        public string place { get; set; }
        public string team1 { get; set; }
        public string team2 { get; set; }
        public State state { get; set; }
    }
}
