namespace Sbobet.Models
{
    public class BuyParams
    {
        public string type = "s";
        public int oid { get; set; }
        public string sel { get; set; }
        public decimal price { get; set; }
        public string act { get; set; }
        public int ps = 4;
        public int pid = 21;
        public int eVersion = 0;
        public string uid { get; set; }

        public int bc { get; set; }
    }
}