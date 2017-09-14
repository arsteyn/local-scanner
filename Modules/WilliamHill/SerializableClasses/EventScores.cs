using System.Xml.Serialization;

namespace WilliamHill.SerializableClasses
{
    public class EventScores
    {
        [XmlAttribute("score_1")]
        public string Score1 { get; set; }

        [XmlAttribute("score_2")]
        public string Score2 { get; set; }
    }
}
