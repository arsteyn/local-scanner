using System.Xml.Serialization;

namespace WilliamHill.SerializableClasses
{
    public class Price
    {
        [XmlAttribute("num")]
        public int Num { get; set; }

        [XmlAttribute("den")]
        public int Den { get; set; }

        [XmlAttribute("frac")]
        public string Frac { get; set; }

        [XmlAttribute("us")]
        public int Us { get; set; }

        [XmlAttribute("dec")]
        public decimal CoeffValue { get; set; }
    }
}
