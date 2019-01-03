using System;
using System.Collections.Generic;
using System.Linq;
using BetFairApi;
using BM.DTO;
using BM.Interfaces;

namespace BetFair
{
    public class BetFairLineConverter : ILineConverter
    {
      
        private const double TotalMatched = 10000;
        
        public static string[] Matches = {
            "MONEY_LINE",
            "TEAM_A",
            "TEAM_B",
            "DOUBLE_CHANCE",
            "MATCH_ODDS",
            "DRAW_NO_BET",
        };

        public LineDTO[] Convert(string response, string bookmaker)
        {
            var token = response.Split('|')[0];
            var appKey = response.Split('|')[1];

            var lines = new List<LineDTO>();

            var aping = new SportsAPING(appKey, token);

            var marketProjection = new List<MarketProjection>
            {
                MarketProjection.EVENT,
                MarketProjection.EVENT_TYPE,
                MarketProjection.RUNNER_DESCRIPTION,
            };


            var marketFilter = new MarketFilter
            {
                //EventTypeIds = new[]
                //{
                //    "1", "7524", "7522", "468328", "998917"
                //},
                InPlayOnly = true,
                //TextQuery = "OVER_UNDER_*",
                MarketBettingTypes = new List<MarketBettingType> { MarketBettingType.ODDS, MarketBettingType.ASIAN_HANDICAP_DOUBLE_LINE }
            };

            var resultList = aping.ListMarketCatalogue(marketFilter, marketProjection, null, 999);

            if (resultList == null || !resultList.Any()) return lines.ToArray();

            var listMarketCatalogue = resultList.ToList();

            marketFilter.TextQuery = null;
            marketFilter.MarketTypeCodes = aping.ListMarketTypes(marketFilter)
                .Select(x => x.MarketType)
                .Where(x => Matches.Any(a => a == x))
                .ToList();

            listMarketCatalogue.AddRange(aping.ListMarketCatalogue(marketFilter, marketProjection, null, 999));

            var marketCataloguesGroups = listMarketCatalogue
                .Where(x => x.Event.Name.Contains(" v ") || x.Event.Name.Contains(" @ "))
                .GroupBy(x => new { EventTypeName = x.EventType.Name, EventName = x.Event.Name });

            lines.AddRange(BetFairHelper.Convert(marketCataloguesGroups.SelectMany(m => m).ToList(), aping, bookmaker));

            return lines.ToArray();
        }
    }

    public class GetCoeffKindParams
    {
        public GetCoeffKindParams(Runner runner, MarketCatalogue marketCatalogue, double? runnerHandicap)
        {
            this.Runner = runner;
            this.MarketCatalogue = marketCatalogue;
            this.Handicap = runnerHandicap;

            var teames = marketCatalogue.Event.Name.ToLower().Split(new[] { " v ", " @ " }, StringSplitOptions.RemoveEmptyEntries);

            this.FirstTeam = teames[0];
            this.SecondTeam = teames[1];

            Mapping[FirstTeam] = "1";
            Mapping[SecondTeam] = "2";
        }

        public Runner Runner { get; set; }

        public MarketCatalogue MarketCatalogue { get; set; }

        public string FirstTeam { get; set; }

        public string SecondTeam { get; set; }

        public double? Handicap { get; set; }

        public Dictionary<string, string> Mapping = new Dictionary<string, string>
            {
                { "Draw or Away" , "X2" },
                { "Home or Draw" , "1X" },
                { "Home or Away" , "12" },
                { "The Draw", "X" }
            };
    }

    public class LineInfo
    {
        public double Size { get; set; }

        public string MarketId { get; set; }

        public long SelectionId { get; set; }
        public double? Handicap { get; set; }
    }
}
