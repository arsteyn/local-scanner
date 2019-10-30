using System.Collections.Generic;

namespace FonBet.SerializedClasses
{
    public class UrlsJson
    {
        public int timeout { get; set; }
        public List<string> common { get; set; }
        public List<string> line { get; set; }
    }
}