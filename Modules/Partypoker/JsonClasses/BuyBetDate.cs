namespace Partypoker.JsonClasses
{
    internal class BuyBetData
    {
        public BuyBetData(Fixture f, Game g, Result o)
        {
            sportId = f.sport.id;
            sportName = f.sport.name.value;
            regionId = f.region.id;
            regionName = f.region.name.value;
            leagueId = f.competition.id;
            leagueName = f.competition.name.value;
            eventId = f.id;
            eventName = f.name.value;
            eventStartsAt = f.startDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            marketId = g.id;
            marketName = g.name.value;
            optionId = o.id;
            optionName = o.name.value;
            signature_event = f.name.sign;
            signature_league = f.competition.name.sign;
            signature_region = f.region.name.sign;
            signature_sport = f.sport.name.sign;
            signature_market = g.name.sign;
            signature_option = o.name.sign;
        }

        public int sportId { get; set; }

        public string sportName { get; set; }

        public int regionId { get; set; }

        public string regionName { get; set; }

        public int leagueId { get; set; }

        public string leagueName { get; set; }

        public long eventId { get; set; }

        public string eventName { get; set; }

        public string eventStartsAt { get; set; }

        public long marketId { get; set; }

        public string marketName { get; set; }

        public long optionId { get; set; }

        public string optionName { get; set; }

        public string signature_event { get; set; }
        public string signature_league { get; set; }
        public string signature_region { get; set; }
        public string signature_sport { get; set; }
        public string signature_market { get; set; }
        public string signature_option { get; set; }
    }
}