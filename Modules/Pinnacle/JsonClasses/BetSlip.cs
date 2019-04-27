namespace Pinnacle.JsonClasses
{
    public class BetSlip
    {
        public PendingTicket PendingTicket { get; set; }

        public string CurrencyCode { get; set; }

        public int ErrorType { get; set; }

        public string Error { get; set; }

        public int SportType { get; set; }
    }
}