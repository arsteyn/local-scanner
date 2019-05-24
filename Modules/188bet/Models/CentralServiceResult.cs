using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bet188.Models
{
    public class CentralServiceResult
    {
        public lpd lpd { get; set; }
        public mbd mbd { get; set; }
    }

    public class mbd
    {
        public d d { get; set; }
    }

    public class d
    {
        /// <summary>
        /// league list
        /// </summary>
        public List<c> c { get; set; }
    }

    public class c
    {
        /// <summary>
        /// event list
        /// </summary>
        public List<e> e { get; set; }
    }

    public class e
    {
        public bool hide { get; set; }
        /// <summary>
        /// event id
        /// </summary>
        public string k { get; set; }
        /// <summary>
        /// event markets container
        /// </summary>
        public o o { get; set; }

        //TODO: здесь double chance
        //[JsonProperty(PropertyName = "n-o")]
        //public List<n_o> n_o { get; set; }
    }

    //public class n_o
    //{
    //    /// <summary>
    //    /// name
    //    /// </summary>
    //    public string n { get; set; }

    //    public List<o> Type { get; set; }

    //}

    public class o
    {
        /// <summary>
        /// Handicap
        /// </summary>
        public oItem ah { get; set; }
        public oItem ah1st { get; set; }

        public oItem ou { get; set; }
        public oItem ou1st { get; set; }


        [JsonProperty(PropertyName = "1x2")]
        public oItem _1x2 { get; set; }
        [JsonProperty(PropertyName = "1x21st")]
        public oItem _1x21st { get; set; }
        public oItem tg { get; set; }
        public oItem tg1st { get; set; }
     
    }

    /// <summary>
    /// Handicap container
    /// </summary>
    public class oItem
    {
        /// <summary>
        /// Name
        /// </summary>
        public string n { get; set; }

        public List<string> v { get; set; }
    }

    public class lpd
    {
        public ips ips { get; set; }
    }

    public class ips
    {
        public List<ismd> ismd { get; set; }
    }

    public class ismd
    {
        /// <summary>
        /// sport id
        /// </summary>
        public int sid { get; set; }

        /// <summary>
        /// sport name
        /// </summary>
        public string sn { get; set; }

        /// <summary>
        /// sport name lower case
        /// </summary>
        public string sen { get; set; }

        public List<puc> puc { get; set; }
    }

    public class puc
    {
        public List<ces> ces { get; set; }

        /// <summary>
        /// league id
        /// </summary>
        public string cid { get; set; }

        /// <summary>
        /// league name
        /// </summary>
        public string cn { get; set; }
    }

    public class ces
    {
        /// <summary>
        /// event id
        /// </summary>
        public string eid { get; set; }
        /// <summary>
        /// home team
        /// </summary>
        public string ht { get; set; }
        /// <summary>
        /// away team
        /// </summary>
        public string at { get; set; }

        /// <summary>
        /// event name for url
        /// </summary>
        public string en { get; set; }

        /// <summary>
        /// home team score
        /// </summary>
        public hs hs { get; set; }

        /// <summary>
        ///away team score 
        /// </summary>
        public hs As { get; set; }

    }

    public class hs
    {
        /// <summary>
        /// score
        /// </summary>
        public int v { get; set; }
    }
}
