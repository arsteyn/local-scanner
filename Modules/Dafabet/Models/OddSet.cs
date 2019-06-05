using System.Collections.Generic;

namespace Dafabet.Models
{
    public class OddSet
    {
        public int enable { get; set; }
        public int bettype { get; set; }
        public decimal cs00 { get; set; }
        public decimal cs10 { get; set; }
        public decimal cs20 { get; set; }
        public decimal comx { get; set; }
        public decimal com1 { get; set; }
        public decimal com2 { get; set; }
        public decimal hdp1 { get; set; }
        public decimal hdp2 { get; set; }
        public long matchid { get; set; }
        public long oddsid { get; set; }
        public decimal odds1a { get; set; }
        public decimal odds2a { get; set; }
        /// <summary>
        /// running
        /// </summary>
        public string oddsstatus { get; set; }
    }

   
}