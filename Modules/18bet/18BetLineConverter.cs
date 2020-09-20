using System;
using System.Collections.Generic;
using System.Linq;
using Bars.EAS.Utils.Extension;
using Bet18.Models;
using BM;
using BM.Core;
using BM.DTO;
using NLog;

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
            "winner",
            //penalty
            "(PEN)",
            //extra time
            "(ET)"
        };

        public LineDTO[] Convert(List<Event> value, string bookmakerName)
        {
            _lines = new List<LineDTO>();

            if (value == null) return new LineDTO[] { };

            foreach (var @event in value)
            {
                //футбол
                if (@event.sport_id != 1 /*|| @event.sport_id != hockey*/) continue;
                if (LeagueStopWords.Any(w => @event.league_title.ContainsIgnoreCase(w))) continue;
                if (@event.is_hidden) continue;
                if (@event.event_status != "running") continue;

                var lineTemplateDto = new LineDTO
                {
                    Team1 = @event.home_team,
                    Team2 = @event.away_team,
                    Score1 = @event.live_score_home,
                    Score2 = @event.live_score_away,
                    SportKind = Helper.ConvertSport(@event.sport_title),
                    BookmakerName = bookmakerName,
                    ObjectCreateDate = DateTime.Now
                };

                ConvertMarkets(lineTemplateDto, @event);
            }


            return _lines.ToArray();
        }

        private void ConvertMarkets(LineDTO lineTemplateDto, Event ev)
        {
            foreach (var evMarket in ev.markets)
            {
                try
                {
                    var market = evMarket.Value;

                    //2 corners
                    //53 yc
                    if (market.line_entity_id != 1) continue;
                    if (market.is_hidden || market.is_suspended) continue;

                    foreach (var marketOdd in market.odds)
                    {
                        var odd = marketOdd.Value;

                        if (odd.o == null) continue;

                        var line = lineTemplateDto.Clone();
                        line.LineData = marketOdd.Key;

                        //TODO: разобрать периоды в хоккее
                        switch (market.game_period_id)
                        {
                            case 1:
                                line.CoeffType = "1st half";
                                break;
                            //Full time
                            case 2:
                                break;
                            case 3:
                                line.CoeffType = "2nd half";
                                break;
                            default:
                                continue;
                        }

                        switch (market.market_key)
                        {
                            case "x12":
                                switch (odd.k)
                                {
                                    case "away":
                                        line.CoeffKind = "2";
                                        break;
                                    case "home":
                                        line.CoeffKind = "1";
                                        break;
                                    case "draw":
                                        line.CoeffKind = "X";
                                        break;
                                }

                                line.CoeffValue = odd.o.Value;
                                AddLine(line);
                                break;
                            case "euro_over_under":
                            case "over_under":
                                switch (odd.k)
                                {
                                    case "over":
                                        line.CoeffKind = "TOTALOVER";
                                        break;
                                    case "under":
                                        line.CoeffKind = "TOTALUNDER";
                                        break;
                                }
                                line.CoeffParam = odd.es;
                                line.CoeffValue = odd.o.Value;
                                AddLine(line);
                                break;
                            case "htt":
                                switch (odd.k)
                                {
                                    case "home_over":
                                        line.CoeffKind = "ITOTALOVER1";
                                        break;
                                    case "home_under":
                                        line.CoeffKind = "ITOTALUNDER1";
                                        break;
                                }
                                line.CoeffParam = odd.es;
                                line.CoeffValue = odd.o.Value;
                                AddLine(line);
                                break;
                            case "att":
                                switch (odd.k)
                                {
                                    case "away_over":
                                        line.CoeffKind = "ITOTALOVER2";
                                        break;
                                    case "away_under":
                                        line.CoeffKind = "ITOTALUNDER2";
                                        break;
                                }
                                line.CoeffParam = odd.es;
                                line.CoeffValue = odd.o.Value;
                                AddLine(line);
                                break;
                            case "handicap":
                                switch (odd.k)
                                {
                                    case "home":
                                        line.CoeffKind = "HANDICAP1";
                                        break;
                                    case "away":
                                        line.CoeffKind = "HANDICAP2";
                                        break;
                                }
                                line.CoeffParam = odd.es;
                                line.CoeffValue = odd.o.Value;
                                AddLine(line);
                                break;
                            case "double_chance":
                                line.CoeffKind = odd.k.ToUpper();
                                line.CoeffValue = odd.o.Value;
                                AddLine(line);
                                break;
                            case "odd_even":
                                line.CoeffKind = odd.k.ToUpper();
                                line.CoeffValue = odd.o.Value;
                                AddLine(line);
                                break;
                            case "draw_no_bet":
                                switch (odd.k)
                                {
                                    case "home":
                                        line.CoeffKind = "W1";
                                        break;
                                    case "away":
                                        line.CoeffKind = "W2";
                                        break;
                                }
                                line.CoeffValue = odd.o.Value;
                                AddLine(line);
                                break;
                        }

                    }
                }
                catch (Exception ex)
                {
                    Log.Info($"ERROR parse event Bet18 {ex.Message} {ex.StackTrace}");
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


