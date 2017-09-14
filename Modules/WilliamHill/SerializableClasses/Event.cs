using System;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Bars.EAS;
using Bars.EAS.Utils;

namespace WilliamHill.SerializableClasses
{
    [XmlRoot("Event")]
    public class Event : BaseElement
    {
        public static readonly Regex TeamRegex = new Regex("^(?<home>.*?) (?:v|@) (?<away>.*?)$");

        [XmlIgnore] 
        private string name;

        [XmlAttribute("name")]
        public override string Name
        {
            get { return name; }

            set
            {
                name = value;
                var match = TeamRegex.Match(name);
                this.Home = match.Groups["home"].Value;
                this.Away = match.Groups["away"].Value;
            }
        }

        [XmlIgnore]
        public DateTime EventDate { get; set; }

        [XmlAttribute("start_time")]
        public string StartTime
        {
            get { return this.EventDate.ToString("yyyy-MM-dd HH:mm:ss"); }
            set
            {
                this.EventDate = DateTime.Parse(value);
            }
        }

        [XmlAttribute("in_running")]
        public string InRunning {get; set;}

        [XmlAttribute("status")]
        public string Status { get; set; }

        [XmlIgnore]
        public string Home { get; set; }

        [XmlIgnore]
        public string Away { get; set; }

        [XmlElement("EventCategory")]
        public BaseElement EventCategory { get; set; }

        [XmlElement("EventClass")]
        public BaseElement EventClass { get; set; }

        [XmlElement("EventState")]
        public EventState EventState { get; set; }

        [XmlIgnore]
        private EventScores eventScores;

        [XmlElement("EventScores")]
        public EventScores EventScores
        {
            get { return eventScores; }
            set
            {
                eventScores = value;

                if (eventScores == null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(eventScores.Score1))
                {
                    var scores = eventScores.Score1.Split('-', '/');
                    Score1 = scores[0].ToInt();
                    Score2 = scores[1].ToInt();    
                }
                
                if (eventScores.Score2.IsNotEmpty())
                {
                    var pscores = eventScores.Score2.Split('-', '/');
                    Pscore1 = pscores[0].ToIntNullable();
                    Pscore2 = pscores[1].ToIntNullable();
                }
            }
        }
        
        [XmlIgnore]
        public int Score1 { get; set; }

        [XmlIgnore]
        public int Score2 { get; set; }

        [XmlIgnore]
        public int? Pscore1 { get; set; }

        [XmlIgnore]
        public int? Pscore2 { get; set; }

        [XmlIgnore]
        private Market[] markets;
        [XmlArray("EventMarkets"), XmlArrayItem("Market")]
        public Market[] Markets
        {
            get { return markets; }

            set
            {
                markets = value;

                for (var i = 0; i < markets.Length; i++)
                {
                    markets[i].Event = this;
                }
            }
        }
    }
}
