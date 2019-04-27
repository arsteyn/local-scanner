namespace Pinnacle.JsonClasses
{
    public class TicketItem
    {
        public decimal? BallPercent { get; set; }
        public int BetType { get; set; }
        public int BuySellLevel { get; set; }
        public long? BuySellId { get; set; }
        public bool DisplayPick { get; set; }
        public bool InDangerZone { get; set; }
        public long EventId { get; set; }
        public EventLine EventLine { get; set; }
        public int? EventFilterType { get; set; }
        public int? EventFilterValue { get; set; }
        public int EventStatus { get; set; }
        public string League { get; set; }
        public long LeagueId { get; set; }
        public string LineDescription { get; set; }
        public long LineId { get; set; }
        public string LineTypeLabel { get; set; }
        public string MarketId { get; set; }
        public decimal? MaxRisk { get; set; }
        public decimal MaxRiskAmount { get; set; }
        public decimal? MinRisk { get; set; }
        public decimal MinRiskAmount { get; set; }
        public decimal? MaxWin { get; set; }
        public decimal? MaxWinAmount { get; set; }
        public decimal? MinWin { get; set; }
        public decimal? MinWinAmount { get; set; }
        public string OverUnder { get; set; }
        public int? Period { get; set; }
        public string PeriodDescription { get; set; }
        public string PeriodShortDescription { get; set; }
        public int? Pick { get; set; }
        public string Sport { get; set; }
        public int? SportId { get; set; }
        public int? SportType { get; set; }
        public string StartDate { get; set; }
        public string Team1FavoriteCss { get; set; }
        public string Team1Id { get; set; }
        public string Team1Name { get; set; }
        public string Team1Pitcher { get; set; }
        public int? Team1RedCards { get; set; }
        public int? Team1Score { get; set; }
        public string Team2FavoriteCss { get; set; }
        public string Team2Id { get; set; }
        public string Team2Name { get; set; }
        public string Team2Pitcher { get; set; }
        public int? Team2RedCards { get; set; }
        public int? Team2Score { get; set; }
        public bool Team1PitcherChecked { get; set; }
        public bool Team2PitcherChecked { get; set; }
        public decimal StakeAmount { get; set; }

    }
}