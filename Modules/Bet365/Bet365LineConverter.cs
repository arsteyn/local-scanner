using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Extensions;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using NLog;
using Scanner;

namespace Bet365
{
    public class Bet365LineConverter
    {
        protected Logger Log => LogManager.GetCurrentClassLogger();

        private List<LineDTO> _lines;

        public static readonly string[] LeagueStopWords = {
            "fantasy",
            "corner",
            "specific",
            "statistics",
            "crossbar",
            "goalpost",
            "fouls",
            "offsides",
            "shot",
            "booking",
            "penalty",
            "special",
            "goal",
            "kick",
            "offside",
            "throw",
            "over",
            "under",
            //penalty
            "(PEN)",
            //extra time
            "(ET)"
        };

        public LineDTO[] Convert(IElement value, string bookmakerName, MarketType type)
        {
            _lines = new List<LineDTO>();

            if (value == null) return new LineDTO[] { };

            var sport = value.QuerySelector("div.ipo-ClassificationHeader_HeaderLabel").Text();

            var leagues = value.QuerySelectorAll("div.ipo-Competition.ipo-Competition-open");

            foreach (var league in leagues)
            {
                var leagueTitle = league.QuerySelector("div.ipo-CompetitionButton_NameLabel").TextContent.Strip();

                if (LeagueStopWords.Any(sw => leagueTitle.ContainsIgnoreCase(sw))) continue;

                var events = league.QuerySelectorAll("div.ipo-Fixture");

                foreach (var ev in events)
                {
                    var teams = ev.QuerySelectorAll("span.ipo-TeamStack_TeamWrapper");

                    var lineTemplateDto = new LineDTO
                    {
                        Team1 = teams[0].TextContent.Strip(),
                        Team2 = teams[1].TextContent.Strip(),
                        Score1 = int.Parse(ev.QuerySelector("div.ipo-TeamPoints_TeamScore.ipo-TeamPoints_TeamScore-teamone").TextContent),
                        Score2 = int.Parse(ev.QuerySelector("div.ipo-TeamPoints_TeamScore.ipo-TeamPoints_TeamScore-teamtwo").TextContent),
                        SportKind = Helper.ConvertSport(sport),
                        BookmakerName = bookmakerName,
                        ObjectCreateDate = DateTime.Now
                    };

                    ConvertMainMarkets(lineTemplateDto, ev, type);

                }

            }

            return _lines.ToArray();
        }

        private void ConvertMainMarkets(LineDTO lineTemplateDto, IElement ev, MarketType marketType)
        {
        //    var columns = ev.QuerySelectorAll("div.ipo-MainMarketRenderer");

        //    for (int i = 0; i < columns.Length; i++)
        //    {
        //        var rows = columns[i].QuerySelectorAll("div.gl-ParticipantCentered:not(.gl-ParticipantCentered_Suspended)");

        //        for (int j = 0; j < rows.Length; j++)
        //        {
        //            var line = lineTemplateDto.Clone();
        //            switch (i)
        //            {


        //                //1X2
        //                case 0 when marketType == MarketType.MainMarkets:
        //                    switch (j)
        //                    {
        //                        case 0:
        //                            line.CoeffKind = "1";

        //                            ///непонятно где брать id исхода
        //                    }
        //                    continue;
                        
                            
                            
        //                    //Asian handicap
        //                case 1 when marketType == MarketType.FullTimeAsians:
        //                    continue;
        //                //Match goals (match total)
        //                case 2 when marketType == MarketType.MainMarkets:

        //                    continue;

        //            }
        //        }

        //    }


        //    foreach (var column in columns)
        //    {
        //        try
        //        {
        //            var oddHolders = column.QuerySelectorAll("a");
        //            var spread = column.QuerySelectorAll("i.spread").ToList();
        //            decimal? coeffparam = null;

        //            int? spreadIndex = null;

        //            var paramExists = spread?.FirstOrDefault(s => s.TextContent.Length > 0);

        //            if (paramExists != null)
        //            {
        //                var split = paramExists.TextContent.Split('-');
        //                coeffparam = split.Length > 1
        //                    ? (decimal.Parse(split[1], CultureInfo.InvariantCulture) + decimal.Parse(split[0], CultureInfo.InvariantCulture)) / 2
        //                    : decimal.Parse(split[0], CultureInfo.InvariantCulture);

        //                spreadIndex = spread.IndexOf(paramExists);
        //            }


        //            foreach (var a in oddHolders)
        //            {
        //                var oddId = a.GetAttribute("id");

        //                if (!oddId.IsNotEmpty()) continue;

        //                var line = lineTemplateDto.Clone();


        //                var odd = a.QuerySelector(".odd").TextContent;

        //                if (string.IsNullOrEmpty(odd)) continue;

        //                line.CoeffValue = decimal.Parse(odd, CultureInfo.InvariantCulture);
        //                line.LineData = oddId;

        //                if (column.ClassName.Contains("column-fh"))
        //                    line.CoeffType = "1st half";

        //                var type = a.GetAttribute("data-pca-autoupdate");
        //                switch (type)
        //                {
        //                    //Full time handicap 
        //                    case "home_1_1_2_2":
        //                    case "home_2_1_2_2":
        //                    case "home_3_1_2_2":

        //                        line.CoeffKind = "HANDICAP1";

        //                        //TODO: ПЕРЕПРОВЕРИТЬ!!!!!!
        //                        if (spreadIndex.HasValue)
        //                            line.CoeffParam = spreadIndex == 0 ? -1 * coeffparam : coeffparam;

        //                        AddLine(line);

        //                        break;
        //                    case "away_1_1_2_2":
        //                    case "away_2_1_2_2":
        //                    case "away_3_1_2_2":

        //                        line.CoeffKind = "HANDICAP2";

        //                        if (spreadIndex.HasValue)
        //                            line.CoeffParam = spreadIndex != 0 ? -1 * coeffparam : coeffparam;

        //                        AddLine(line);

        //                        break;

        //                    case "over_1_1_2_3":
        //                    case "over_2_1_2_3":
        //                    case "over_3_1_2_3":
        //                        line.CoeffKind = "TOTALOVER";

        //                        if (spreadIndex.HasValue)
        //                            line.CoeffParam = coeffparam;

        //                        AddLine(line);

        //                        break;

        //                    case "under_1_1_2_3":
        //                    case "under_2_1_2_3":
        //                    case "under_3_1_2_3":
        //                        line.CoeffKind = "TOTALUNDER";

        //                        if (spreadIndex.HasValue)
        //                            line.CoeffParam = coeffparam;

        //                        AddLine(line);

        //                        break;

        //                    case "home_1_1_2_1":
        //                        line.CoeffKind = "1";

        //                        AddLine(line);

        //                        break;
        //                    case "away_1_1_2_1":
        //                        line.CoeffKind = "2";

        //                        AddLine(line);

        //                        break;
        //                    case "draw_1_1_2_1":
        //                        line.CoeffKind = "X";

        //                        AddLine(line);

        //                        break;
        //                }
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Info($"ERROR parse Bet18 {ex.Message} {ex.StackTrace}");
        //        }
        //    }
        //}

    }


    private void AddLine(LineDTO lineDto)
    {
        //lineDto.UpdateName();
        //_lines.Add(lineDto);
    }
}

public enum MarketType
{
    MainMarkets = 0,
    FirstHalfAsians = 1,
    FullTimeAsians = 2
}
}


