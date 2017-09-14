using System.Collections.Generic;
using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class Market
    {
        [JsonProperty(PropertyName = "markets")]
        public List<Sport> Sports { get; set; }
    }
}
