namespace Pinnacle.JsonClasses
{
    public class PendingTicket
    {
        public bool BetMaximum { get; set; }

        public bool AcceptBetterLines { get; set; }

        public long BetTicketId { get; set; }

        public Error Error { get; set; }

        public bool IsAccepted { get; set; }

        public bool IsEmpty { get; set; }

        public Notification Notification { get; set; }

        public string RefreshLink { get; set; }

        public TicketItem TicketItem { get; set; }

        public int TicketSource { get; set; }

        public string UniqueId { get; set; }

        public bool IsWaiting { get; set; }
    }
}