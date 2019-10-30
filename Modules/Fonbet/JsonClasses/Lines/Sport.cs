using Newtonsoft.Json;

namespace FonBet.SerializedClasses
{
    public class Sport
    {
        public int id { get; set; }

        [JsonProperty("parentId")]
        public int? ParentId { get; set; }
        public string kind { get; set; }
        public int regionId { get; set; }
        public string sortOrder { get; set; }
        public string name { get; set; }
    }
}