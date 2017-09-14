using System.Xml.Serialization;

namespace WilliamHill.SerializableClasses
{
    public class Selection : BaseElement
    {
        [XmlAttribute("status")]
        public string Status { get; set; }

        [XmlAttribute("tag")]
        public string Tag { get; set; }

        [XmlElement("Price")]
        public Price Price { get; set; }

        [XmlElement("HcapInfo")]
        public HcapInfo HcapInfo { get; set; }
    }
}
