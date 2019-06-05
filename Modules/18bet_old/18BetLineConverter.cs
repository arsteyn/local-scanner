using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using Newtonsoft.Json;
using NLog;
using OpenQA.Selenium;
using Scanner;

namespace Bet18
{
    public class Bet18LineConverter
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

        public LineDTO[] Convert(IElement value, string bookmakerName)
        {
            _lines = new List<LineDTO>();

            if (value == null) return new LineDTO[] { };

            var sport = value.ClassName.Split(' ')[1].Split('-')[1];

            var leagues = value.QuerySelectorAll(".league-container");

            foreach (var league in leagues)
            {
                var leagueTitle = league.QuerySelector(".league-title").TextContent.Strip();

                if (LeagueStopWords.Any(sw => leagueTitle.ContainsIgnoreCase(sw))) continue;

                var events = league.QuerySelectorAll("div.event.event-container.event-container");

                foreach (var ev in events)
                {
                    var teamHome = ev.QuerySelector("span[data-pca-autoupdate='home_team']").TextContent.Strip();
                    var teamAway = ev.QuerySelector("span[data-pca-autoupdate='away_team']").TextContent.Strip();
                    var score1span = ev.QuerySelector("span[data-pca-autoupdate='live_score_home']");
                    var score2span = ev.QuerySelector("span[data-pca-autoupdate='live_score_away']");

                    if (score1span == null || score2span == null) continue;

                    var lineTemplateDto = new LineDTO
                    {
                        Team1 = teamHome,
                        Team2 = teamAway,
                        Score1 = int.Parse(score1span.TextContent.Strip()),
                        Score2 = int.Parse(score2span.TextContent.Strip()),
                        SportKind = Helper.ConvertSport(sport),
                        BookmakerName = bookmakerName,
                        ObjectCreateDate = DateTime.Now
                    };

                    ConvertBasicAsianView(lineTemplateDto, ev);

                }

            }

            return _lines.ToArray();
        }

        private void ConvertBasicAsianView(LineDTO lineTemplateDto, IElement ev)
        {
            var mainMarkets = ev.QuerySelectorAll(".main-market-option");

            foreach (var market in mainMarkets)
            {
                var columns = market.QuerySelectorAll(".market-item-column");

                foreach (var column in columns)
                {
                    try
                    {
                        var oddHolders = column.QuerySelectorAll("span.odd-holder");
                        var spread = column.QuerySelectorAll("i.spread").ToList();
                        decimal? coeffparam = null;

                        int? spreadIndex = null;

                        var paramExists = spread?.FirstOrDefault(s => s.TextContent.Length > 0);

                        if (paramExists != null)
                        {
                            var split = paramExists.TextContent.Split('-');
                            coeffparam = split.Length > 1
                                ? (decimal.Parse(split[1], CultureInfo.InvariantCulture) + decimal.Parse(split[0], CultureInfo.InvariantCulture)) / 2
                                : decimal.Parse(split[0], CultureInfo.InvariantCulture);

                            spreadIndex = spread.IndexOf(paramExists);
                        }


                        foreach (var oddHolder in oddHolders)
                        {
                            var oddId = oddHolder.GetAttribute("id");

                            if (!oddId.IsNotEmpty()) continue;

                            var line = lineTemplateDto.Clone();


                            var odd = oddHolder.QuerySelector(".odd").TextContent;

                            if (string.IsNullOrEmpty(odd)) continue;

                            line.CoeffValue = decimal.Parse(odd, CultureInfo.InvariantCulture);
                            line.LineData = oddId;

                            if (column.ClassName.Contains("column-fh"))
                                line.CoeffType = "1st half";

                            var type = oddHolder.GetAttribute("data-pca-autoupdate");
                            switch (type)
                            {
                                //Full time handicap 
                                case "home_1_1_2_2":
                                case "home_2_1_2_2":
                                case "home_3_1_2_2":

                                    line.CoeffKind = "HANDICAP1";

                                    //TODO: ПЕРЕПРОВЕРИТЬ!!!!!!
                                    if (spreadIndex.HasValue)
                                        line.CoeffParam = spreadIndex == 0 ? -1 * coeffparam : coeffparam;

                                    AddLine(line);

                                    break;
                                case "away_1_1_2_2":
                                case "away_2_1_2_2":
                                case "away_3_1_2_2":

                                    line.CoeffKind = "HANDICAP2";

                                    if (spreadIndex.HasValue)
                                        line.CoeffParam = spreadIndex != 0 ? -1 * coeffparam : coeffparam;

                                    AddLine(line);

                                    break;

                                case "over_1_1_2_3":
                                case "over_2_1_2_3":
                                case "over_3_1_2_3":
                                    line.CoeffKind = "TOTALOVER";

                                    if (spreadIndex.HasValue)
                                        line.CoeffParam = coeffparam;

                                    AddLine(line);

                                    break;

                                case "under_1_1_2_3":
                                case "under_2_1_2_3":
                                case "under_3_1_2_3":
                                    line.CoeffKind = "TOTALUNDER";

                                    if (spreadIndex.HasValue)
                                        line.CoeffParam = coeffparam;

                                    AddLine(line);

                                    break;

                                case "home_1_1_2_1":
                                    line.CoeffKind = "1";

                                    AddLine(line);

                                    break;
                                case "away_1_1_2_1":
                                    line.CoeffKind = "2";

                                    AddLine(line);

                                    break;
                                case "draw_1_1_2_1":
                                    line.CoeffKind = "X";

                                    AddLine(line);

                                    break;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Info($"ERROR parse Bet18 {ex.Message} {ex.StackTrace}");
                    }
                }
            }

        }


        private void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            _lines.Add(lineDto);
        }
    }

}


