using System.Collections.Generic;
using System.Net;

namespace WilliamHill.Models
{
    public class SlipData
    {
        public string ev_oc_id { get; set; }

        public string lp_num { get; set; }
        public string lp_den { get; set; }
        public string hcap_value { get; set; }

        public string bet_uid { get; set; }
        public List<Cookie> Cookies { get; set; }
    }
}
