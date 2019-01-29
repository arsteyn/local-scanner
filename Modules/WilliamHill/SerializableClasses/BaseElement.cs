using System.Xml.Serialization;

namespace WilliamHill.SerializableClasses
{
    public class BaseElement
    {
        [XmlAttribute("ob_id")]
        public long Id { get; set; }

        [XmlAttribute("name")]
        public virtual string Name { get; set; }
    }
}
