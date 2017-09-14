using System.Xml.Serialization;

namespace WilliamHill.SerializableClasses
{
    public class HcapInfo
    {
        [XmlElement("HcapString")]
        public string HcapString { get; set; }

        [XmlElement("HcapBlurb")]
        public string HcapBlurb { get; set; }
    }
}
