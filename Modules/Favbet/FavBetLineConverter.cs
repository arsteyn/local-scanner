
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Bars.EAS.Utils;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using Favbet.Models;
using Favbet.Models.Line;
using Newtonsoft.Json;
using NLog;

namespace Favbet
{
    public class FavBetConverter
    {
        protected Logger Log => LogManager.GetCurrentClassLogger();

        public readonly Regex TeamRegex = new Regex(@"^(?<home>.*?) - (?<away>.*?)$");
        public readonly Regex ScoreRegex = new Regex(@"(?<score1>\d+):(?<score2>\d+)(?:.*?[(].*?(?<pscore1>\d+):(?<pscore2>\d+)[)])?(?: \d+')?");

        public static Dictionary<string, string> MapRuls = new Dictionary<string, string>
                {
                    // {Искомая_строка_1@Искомая_строка_2@ ... @Искомая_строка_n@Регулярка_для_обработки}, {ставки}.
                    {@"match winner", "1|2"},
                    {@"1 x 2@money line", "1|X|2"},
                    {@"draw no bet@2-way odds@set winner", "W1|W2"},
                    {@"double chance", "1X|12|X2"},
                    {@"handicap", "HANDICAP1|PARAM|HANDICAP2"},
                    {@"over/under", "TOTALOVER|PARAM|TOTALUNDER"},
                    {@"team total@player total games", "ITOTALOVER|PARAM|ITOTALUNDER"},
                    {@"odd / even", "ODD|EVEN"}
                };


        public LineDTO CreateLine(Event @event, string host, string name)
        {
            try
            {
                if (@event.event_status_type != "inprogress") return null;

                @event.sport_name = Helper.ConvertSport(@event.sport_name);

                var teamMatch = TeamRegex.Match(@event.event_name);
                var scoreMatch = ScoreRegex.Match(@event.event_result_total);

                var line = new LineDTO
                {
                    BookmakerName = name,
                    SportKind = @event.sport_name,
                    Team1 = teamMatch.Groups["home"].Value,
                    Team2 = teamMatch.Groups["away"].Value,
                    Url = $"{host}en/live/#event={@event.event_id}",
                    Score1 = scoreMatch.Groups["score1"].Value.ToInt(),
                    Score2 = scoreMatch.Groups["score2"].Value.ToInt(),
                    CoeffType = @event.event_result_name.ToLower(),
                    Pscore1 = scoreMatch.Groups["pscore1"].Value.ToIntNullable(),
                    Pscore2 = scoreMatch.Groups["pscore1"].Value.ToIntNullable(),
                    EventDate = DateTime.Now,
                    LineObject = @event.event_id.ToString()
                };

                //Добавляем возраст если играют молодежные команды
                const string regex = @"U[1-2][0-9]";
                var ageRegex = new Regex(regex).Match(@event.tournament_name);

                if (!ageRegex.Success) return line;

                var age = ageRegex.Value;
                line.Team1 += " " + age;
                line.Team2 += " " + age;

                return line;
            }
            catch (Exception e)
            {
                Log.Info("Favbet CreateLine exception " + JsonConvert.SerializeObject(e));
            }

            return null;
        }

        public List<LineDTO> GetLinesFromEvent(LineDTO template, List<Market> markets)
        {
            var localLines = new List<LineDTO>();

            foreach (var market in markets)
            {
                var copy = template.Clone();

                var localGameType = string.Empty;

                // Извлекаем тип игры: угловые, (желтые, красные карты), картер, и т.д.
                if (string.IsNullOrEmpty(localGameType)) localGameType = ConverterHelper.GetGameType(market.market_name);

                if (!ConverterHelper.GetPeriod(market.result_type_name, out var period)) continue;

                copy.CoeffType = $"{period} {localGameType}";

                if (!CheckExceptions(copy, market)) continue;

                foreach (var outcome in market.outcomes)
                {
                    if (!outcome.outcome_visible) continue;

                    var line = copy.Clone();

                    // Номер команды

                    if (market.market_name.ContainsIgnoreCase("match winner", "1 X 2", "Double chance", "Odd / Even"))
                    {
                        line.CoeffKind = outcome.outcome_short_name;
                    }
                    else if (market.market_name.ContainsIgnoreCase("Draw No Bet"))
                    {
                        line.CoeffKind = $"W{outcome.outcome_short_name}";
                    }
                    else if (market.market_name.ContainsIgnoreCase("Draw No Bet"))
                    {
                        line.CoeffKind = $"W{outcome.outcome_short_name}";
                    }
                    else if (market.market_name.ContainsIgnoreCase("Handicap"))
                    {
                        line.CoeffKind = $"HANDICAP{outcome.participant_number}";
                    }
                    else if (market.market_name.ContainsIgnoreCase("Over/Under"))
                    {
                        line.CoeffKind = $"TOTAL{outcome.outcome_name.Split(" ")[0].ToUpper()}";
                    }
                    else if (market.market_name.ContainsIgnoreCase("Team total"))
                    {
                        line.CoeffKind = $"ITOTAL{outcome.outcome_name.Split(" ")[0].ToUpper()}{outcome.participant_number}";
                    }
                    else if (market.market_name.ContainsIgnoreCase("Odd / Even"))
                    {
                        line.CoeffKind = $"ITOTAL{outcome.outcome_name.Split(" ")[0].ToUpper()}{outcome.participant_number}";
                    }
                    else
                    {
                        continue;
                    }

                    //Параметр 
                    if (decimal.TryParse(outcome.outcome_param, NumberStyles.Any, CultureInfo.InvariantCulture, out var param))
                    {
                        line.CoeffParam = param;
                    }

                    line.CoeffValue = outcome.outcome_coef;

                    line.LineData = new StoreBet { OutcomeId = outcome.outcome_id.ToLong(), EventId = market.event_id.ToLong(), MarketId = market.market_id.ToLong() };

                    line.UpdateName();

                    localLines.Add(line);
                }
            }

            return localLines;

        }

        public bool CheckExceptions(LineDTO line, Market market)
        {
            //исключаем индивидуальные тоталы в футболе
            if (line.SportKind.ContainsAllIgnoreCase("football") && market.market_name.ContainsIgnoreCase("player"))
                return false;

            //исключаем тоталы 3way
            if (market.market_name.ContainsIgnoreCase("3way"))
                return false;

            //исключаем гандикапы по сетам и геймам
            if (market.market_name.ContainsAllIgnoreCase("sets handicap") || market.market_name.ContainsAllIgnoreCase("games handicap"))
                return false;

            //Только у FavBet тоталы на баскетбол на последнюю четверть считаются с учетом овертайма, в отличии от других контор
            if (line.SportKind.ContainsAllIgnoreCase("basketball") && line.CoeffType == "4th quarter" && line.CoeffKind.Contains("TOTAL"))
                return false;

            return true;
        }
    }
}


