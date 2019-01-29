using System.Collections.Generic;

namespace Dafabet.Models
{
    internal class OddSet
    {
        public long OddsId { get; set; }

        public int Bettype { get; set; }

        public int MarketStatus { get; set; }

        public List<Select> sels { get; set; }
    }
}