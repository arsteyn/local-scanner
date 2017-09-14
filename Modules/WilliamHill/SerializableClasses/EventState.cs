using System;
using System.Xml.Serialization;

namespace WilliamHill.SerializableClasses
{
    public class EventState
    {
        [XmlAttribute("state")]
        public string State { get; set; }

        [XmlIgnore]
        public DateTime? LastUpdate { get; set; }

        [XmlAttribute("updated")]
        public string Updated
        {
            get { return this.LastUpdate.HasValue ? this.LastUpdate.Value.ToString("yyyy-MM-dd HH:mm:ss") : null; }
            set
            {
                DateTime dataTime;
                DateTime.TryParse(value, out dataTime);
                this.LastUpdate = dataTime;
            }
        }
    }
}
