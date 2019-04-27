using System;
using System.Web;

namespace Pinnacle.JsonClasses
{
    public class GameLine
    {
        
        public Team TmH { get; set; }
        public Team TmA { get; set; }
        public string TmD { get; set; }
        public int Rot { get; set; }
        public long EvId { get; set; }
        public int Lvl { get; set; }
        public DateTime Date { get; set; }
        public string[] WagerMap { get; set; }
        public string[] SpWagerMap { get; set; }
        public string[] TotWagerMap { get; set; }

        private string _dispDate;
        public string DispDate
        {
            get { return HttpUtility.HtmlDecode(Uri.UnescapeDataString(_dispDate)); }
            set { _dispDate = value; }
        }
    }

    public class Team
    {
        public string Txt { get; set; }

        public string Fc { get; set; }
    }
}