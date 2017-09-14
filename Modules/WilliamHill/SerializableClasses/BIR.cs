using System.Xml.Serialization;

namespace WilliamHill.SerializableClasses
{
    [XmlRoot("BIR")]
    public class BIR
    {
        [XmlElement("Event")]
        public Event Event { get; set; }
    }
}
