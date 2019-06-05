using System.Collections.Generic;

namespace Dafabet.Models
{
    public class OddSet
    {
        public OddSet()
        {
            this.sels = new List<Select>();
        }

        public long OddsId { get; set; }

        public int Bettype { get; set; }

        public List<Select> sels { get; set; }
    }
}