using System.Xml.Serialization;

namespace WilliamHill.SerializableClasses
{
    public class Market : BaseElement
    {
        [XmlIgnore]
        public Event Event { get; set; }

        /// <summary>
        /// Тип коэффициента
        /// </summary>
        [XmlAttribute("template")]
        public string Template { get; set; }

        [XmlAttribute("hcap")]
        public string Hcap { get; set; }

        [XmlAttribute("status")]
        public string Status { get; set; }

        [XmlArray("MarketSelections"), XmlArrayItem("Selection")]
        public Selection[] Selections { get; set; }
    }
}
