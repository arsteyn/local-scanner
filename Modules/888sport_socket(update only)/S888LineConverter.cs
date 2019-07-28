using System;
using System.Collections.Generic;
using System.Linq;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using Newtonsoft.Json;
using NLog;
using S888.Models.Line;

namespace S888
{
    public class S888LineConverter
    {
        protected Logger Log => LogManager.GetCurrentClassLogger();

        public LineDTO CreateLine(EventSub @event, string host, string name)
        {
            try
            {
                if (!@event.Event.state.EqualsIgnoreCase("started")) return null;

                @event.Event.sport = Helper.ConvertSport(@event.Event.sport);

                var s1 = 0;
                var s2 = 0;
                if (@event.LiveData?.score != null && (!int.TryParse(@event.LiveData.score.home, out s1) ||
                                                       !int.TryParse(@event.LiveData.score.away, out s2)))
                    return null;


                var line = new LineDTO
                {
                    BookmakerName = name,
                    SportKind = @event.Event.sport,
                    Team1 = @event.Event.homeName,
                    Team2 = @event.Event.awayName,
                    Url = $"{host}offering/v2018/888/betoffer/event/{@event.Event.id}.json?lang=en_GB&market=en",
                    Score1 = s1,
                    Score2 = s2,
                    //CoeffType = @event.event_result_name.ToLower(),
                    EventDate = DateTime.Now,
                    LineObject = JsonConvert.SerializeObject(@event)
                };

                return line;
            }
            catch (Exception e)
            {
                Log.Info("888sport CreateLine exception " + JsonConvert.SerializeObject(e) + Environment.NewLine + JsonConvert.SerializeObject(@event));
            }

            return null;
        }

        public List<LineDTO> GetLinesFromEvent(LineDTO template, EventFull @event)
        {
            var localLines = new List<LineDTO>();

            var simpleMap = new Dictionary<string, string>
            {
                {template.Team1, "1"},
                {template.Team2, "2"},
            };

            foreach (var offer in @event.BetOffers.Where(bo => !bo.suspended))
            {
                var copy = template.Clone();

                // Извлекаем тип игры: угловые, (желтые, красные карты), картер, и т.д.

                //if (!ConverterHelper.CheckCriterion(offer.criterion, out var period)) continue;

                foreach (var outcome in offer.outcomes)
                {
                    if (outcome.status != "OPEN") continue;

                    var line = copy.Clone();

                    switch (offer.betOfferType.englishName)
                    {
                        case "Match":
                            switch (offer.criterion.englishLabel)
                            {
                                case "Full Time":
                                case "1st Half":
                                case "2nd Half":
                                case "Half Time":
                                case "Period 1":
                                case "Period 2":
                                case "Period 3":
                                case "2nd Half (3-way)":
                                case "Including Overtime":
                                case "Quarter 1":
                                case "Quarter 2":
                                case "Quarter 3":
                                case "Quarter 4":

                                    line.CoeffKind = outcome.englishLabel;
                                    break;
                                case "Draw No Bet":
                                case "Draw No Bet - 1st Half":
                                case "Draw No Bet - 2nd Half":
                                case "Draw No Bet - Regular Time":
                                case "Draw No Bet - Quarter 1":
                                case "Draw No Bet - Quarter 2":
                                case "Draw No Bet - Quarter 3":
                                case "Draw No Bet - Quarter 4":
                                case "Draw No Bet - Period 1":
                                case "Draw No Bet - Period 2":
                                case "Draw No Bet - Period 3":

                                    line.CoeffKind = "W" + outcome.englishLabel;
                                    break;

                                default:
                                    continue;
                            }
                            break;
                        case "Handicap":
                        case "Asian Handicap":
                            if (offer.criterion.englishLabel == "Handicap" ||
                                offer.criterion.englishLabel == "Handicap - 1st Half" ||
                                offer.criterion.englishLabel == "Handicap - Quarter 1" ||
                                offer.criterion.englishLabel == "Handicap - Quarter 2" ||
                                offer.criterion.englishLabel == "Handicap - Quarter 3" ||
                                offer.criterion.englishLabel == "Handicap - Quarter 4" ||
                                offer.criterion.englishLabel == "Handicap - Including Overtime" ||
                                offer.criterion.englishLabel == "Handicap - Period 1" ||
                                offer.criterion.englishLabel == "Handicap - Period 2" ||
                                offer.criterion.englishLabel == "Handicap - Period 3" ||
                                offer.criterion.englishLabel == "Handicap - Regular Time")
                                line.CoeffKind = "HANDICAP" + simpleMap[outcome.englishLabel];

                            //TODO:разобраться как отделить азиатские гандикапы от обычных
                            //https://www.888sport.com/en/getting-started/betting-rules/
                            //else if (Regex.IsMatch(offer.criterion.englishLabel, "Asian Handicap \\([0-9] - [0-9]\\)"))
                            //{
                            //    line.CoeffKind = "HANDICAP" + simpleMap[outcome.englishLabel];
                            //}
                            else
                            {
                                continue;
                            }
                            break;
                        case "Over/Under":
                        case "Asian Over/Under":
                            switch (offer.criterion.englishLabel)
                            {
                                case "Total Points - 1st Half":
                                case "Total Points - Quarter 1":
                                case "Total Points - Quarter 2":
                                case "Total Points - Quarter 3":
                                case "Total Points - Quarter 4":
                                case "Total Points - Including Overtime":
                                case "Total Goals":
                                case "Total Goals - 1st Half":
                                case "Total Goals - Period 1":
                                case "Total Goals - Period 2":
                                case "Total Goals - Period 3":
                                case "Total Goals - Regular Time":
                                    line.CoeffKind = "TOTAL" + outcome.englishLabel;
                                    break;
                                case "Asian Total":
                                case "Asian Total - 1st Half":
                                    line.CoeffKind = "TOTAL" + outcome.englishLabel;
                                    break;
                                case "Total Goals by Away Team":
                                case "Total Goals by Away Team - 1st Half":
                                case "Total Goals by Away Team - 2nd Half":
                                case "Total Goals by Away Team - Period 1":
                                case "Total Goals by Away Team - Period 2":
                                case "Total Goals by Away Team - Period 3":
                                case "Total Goals by Away Team - Regular Time":
                                    line.CoeffKind = "ITOTAL" + outcome.englishLabel + "2";
                                    break;
                                case "Total Goals by Home Team":
                                case "Total Goals by Home Team - 1st Half":
                                case "Total Goals by Home Team - 2nd Half":
                                case "Total Goals by Home Team - Period 1":
                                case "Total Goals by Home Team - Period 2":
                                case "Total Goals by Home Team - Period 3":
                                case "Total Goals by Home Team - Regular Time":
                                    line.CoeffKind = "ITOTAL" + outcome.englishLabel + "1";
                                    break;
                                default:
                                    continue;
                            }
                            break;
                        case "Double Chance":
                            switch (offer.criterion.englishLabel)
                            {
                                case "Double Chance":
                                case "Double Chance - Period 1":
                                case "Double Chance - Period 2":
                                case "Double Chance - Period 3":
                                    line.CoeffKind = outcome.englishLabel;
                                    break;
                                default:
                                    continue;
                            }
                            break;
                        case "Odd/Even":
                            switch (offer.criterion.englishLabel)
                            {
                                case "Total Goals Odd/Even":
                                case "Total Goals Odd/Even - 1st Half":
                                case "Total Goals Odd/Even - 2nd Half":
                                case "Total Points Odd/Even - Including Overtime":
                                    line.CoeffKind = outcome.englishLabel;
                                    break;
                                default:
                                    continue;
                            }
                            break;
                        default:
                            continue;
                    }

                    //Параметр 
                    if (outcome.line != null)
                    {
                        line.CoeffParam = Math.Round(outcome.line.Value / 1000m, 2);
                    }

                    line.CoeffType = ConverterHelper.GetPeriod(offer.criterion.englishLabel);

                    line.CoeffValue = Math.Round(outcome.odds / 1000m, 2);
                    line.LineData = outcome.odds + ";" + outcome.id;
                    line.LineObject = "| Outcome | " + JsonConvert.SerializeObject(outcome) + " | Offer | " + JsonConvert.SerializeObject(offer);
                    line.UpdateName();
                    localLines.Add(line);
                }
            }

            return localLines;

        }

    }

}


