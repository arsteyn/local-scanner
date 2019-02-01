using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using JsonClasses;
using Leonbets.JsonClasses;
using Newtonsoft.Json;
using NLog;
using Scanner;
using Scanner.Helper;

namespace Leonbets
{
    public class LeonBetsLineConverter
    {
        protected  Logger Log => LogManager.GetCurrentClassLogger();

        private static readonly Regex TeamRegex = new Regex("^(?<home>.*?) - (?<away>.*?)$");
        private static readonly Regex ScoreRegex = new Regex("^(?<homeScore>.*?):(?<awayScore>.*?)$");

        public  List<LineDTO> Lines;

        internal static readonly List<string> StopWords = new List<string>
        {
            "totals", "number", "corner", "card", "booking", "penal"
        };

        //Convert single event
        public  LineDTO[] Convert(Event @event, string bookmakerName)
        {
            Lines = new List<LineDTO>();
            var teamMatch = TeamRegex.Match(@event.name);
            var teamScore = ScoreRegex.Match(@event.liveStatus.score.Replace("*", ""));


            var lineTemplate = new LineDTO
            {
                Team1 = teamMatch.Groups["home"].Value,
                Team2 = teamMatch.Groups["away"].Value,
                BookmakerName = bookmakerName,
                Score1 = System.Convert.ToInt32(teamScore.Groups["homeScore"].Value),
                Score2 = System.Convert.ToInt32(teamScore.Groups["awayScore"].Value),
                SportKind = Helper.ConvertSport(@event.league.sport.name),
                EventDate = TimeExt.FromUnixTime(@event.kickoff)
            };

            foreach (var market in @event.markets)
            {
                //исключаем угловые
                if (StopWords.Any(s => market.name.ContainsIgnoreCase(s))) continue;

                foreach (var runner in market.runners)
                {
                    try
                    {
                        Convert(@event, market, runner, lineTemplate);
                    }
                    catch (Exception e)
                    {
                        Log.Info("LeonBets error" + e.Message + " " + e.StackTrace + (e.InnerException != null ? e.InnerException.Message : ""));
                    }
                }
            }
            return Lines.ToArray();
        }

        private void Convert(Event @event, Market market, Runner runner, LineDTO lineTemplate)
        {
            var line = lineTemplate.Clone();

            var kind = GetCoeffKind(line.SportKind, runner, market);

            if (string.IsNullOrEmpty(kind)) return;

            line.CoeffKind = kind;

            decimal coeffValue;

            decimal.TryParse(runner.price, NumberStyles.Any, CultureInfo.InvariantCulture, out coeffValue);

            line.CoeffValue = coeffValue;

            //в runner.name приходит значение иногда с минусом иногда с плюсом
            if (runner.name.Contains("(") && runner.name.Contains(")"))
            {
                decimal.TryParse(runner.name.Split('(', ')')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var coeffParam);
                line.CoeffParam = coeffParam;
            }

            line.CoeffType = GetCoeffType(line.SportKind, market);

            line.LineData = new
            {
                eventId = @event.Id,
                odd = coeffValue,
                marketId = market.id,
                runnerId = runner.id,
            };

            //line.LineObject = odd.specialOddsValue;

            AddLine(line);
        }

        private static string GetCoeffType(string sportKind, Market market)
        {
            var typeResult = string.Empty;

            var marketNameLower = market.name.ToLower();

            if (market.name.Contains("period"))
            {

                string periodType;

                switch (sportKind)
                {
                    case "BASKETBALL":
                        periodType = "quarter";
                        break;
                    case "VOLLEYBALL":
                        periodType = "set";
                        break;
                    default:
                        periodType = "period";
                        break;
                }

                Helper.ReplaceCoeffTypes(market.name, out var period);

                var coeffType = $"{period} {periodType}";

                return coeffType;
            }

            if (marketNameLower.ContainsOneOf("1st half", "first half", "halftime"))
            {
                typeResult += "1st half";
            }

            if (marketNameLower.ContainsOneOf("2nd half", "second half"))
            {
                typeResult += "2nd half";
            }

            if (marketNameLower.ContainsOneOf("overtime", "inc."))
            {
                typeResult += "inc. OT";
            }

            return typeResult;
        }

        private static string GetCoeffKind(string sportKind, Runner runner, Market market)
        {
            //ProxyHelper.UpdateLeonEvents(sportKind + " | " + market.name + " | " + runner.name);

            if (sportKind == "Basketball")
            {
                if (market.name == "Odd/Even" || market.name == "1X2") return string.Empty;
                if (market.name.StartsWith("Asian", StringComparison.OrdinalIgnoreCase)) return string.Empty;
                if (market.name == "Total") return string.Empty;
            }

            if (
                   market.name.EqualsIgnoreCase("1X2")
                || market.name.EqualsIgnoreCase("Halftime: 3way")
                || market.name.EqualsIgnoreCase("3way for first period")
                || market.name.EqualsIgnoreCase("3way for fourth period")
                || market.name.EqualsIgnoreCase("3way for second period")
                || market.name.EqualsIgnoreCase("3way for third period")
                || market.name.EqualsIgnoreCase("2nd Half: 3way")
                || market.name.EqualsIgnoreCase("Double chance (1X:12:X2)")
                || market.name.EqualsIgnoreCase("Halftime: Double chance (1X:12:X2)")
                || market.name.EqualsIgnoreCase("2nd Half - Double chance (1X - 12 - X2)")
                || market.name.EqualsIgnoreCase("Odd/Even")
                || market.name.EqualsIgnoreCase("Odd/Even for first period")
                || market.name.EqualsIgnoreCase("Odd/Even for second period")
                || market.name.EqualsIgnoreCase("Odd/Even for third period")
                || market.name.EqualsIgnoreCase("Odd/Even for fourth period")
                || market.name.EqualsIgnoreCase("Odd/Even for fifth period")
                || market.name.EqualsIgnoreCase("Odd/Even for whole match including overtime")
                || market.name.EqualsIgnoreCase("Odd/Even for first half")
                || market.name.EqualsIgnoreCase("2nd Half - Odd/Even, including overtime")
                )
            {
                return runner.name.ToUpper();
            }

            if (
                   market.name.EqualsIgnoreCase("Total hometeam")
                || market.name.EqualsIgnoreCase("Asian total hometeam")
                || market.name.EqualsIgnoreCase("Asian total hometeam for the whole match, including overtime")
                )
            {
                if (runner.name.StartsWithIgnoreCase("under")) return "ITOTALUNDER1";
                if (runner.name.StartsWithIgnoreCase("over")) return "ITOTALOVER1";
            }

            else if (
                   market.name.EqualsIgnoreCase("Total awayteam")
                || market.name.EqualsIgnoreCase("Total awayteam including overtime")
                || market.name.EqualsIgnoreCase("Asian total awayteam")
                || market.name.EqualsIgnoreCase("Asian total awayteam for the whole match, including overtime")
                )
            {
                if (runner.name.StartsWithIgnoreCase("under")) return "ITOTALUNDER2";
                if (runner.name.StartsWithIgnoreCase("over")) return "ITOTALOVER2";
            }

            else if (
                   market.name.EqualsIgnoreCase("2nd Half - Asian handicap, including overtime")
                || market.name.EqualsIgnoreCase("Asian handicap (inc. РћРў)")
                || market.name.EqualsIgnoreCase("Asian Handicap")
                || market.name.EqualsIgnoreCase("Asian handicap first half")
                || market.name.EqualsIgnoreCase("Asian handicap for fifth period")
                || market.name.EqualsIgnoreCase("Asian handicap for second period")
                || market.name.EqualsIgnoreCase("Asian handicap for third period")
                || market.name.EqualsIgnoreCase("Asian handicap for fourth period")
                )
            {
                return "HANDICAP" + runner.name[0];
            }

            else if (
                   market.name.EqualsIgnoreCase("Draw No Bet")
                || market.name.EqualsIgnoreCase("Draw No Bet first half")
                || market.name.EqualsIgnoreCase("Draw No Bet for first period")
                || market.name.EqualsIgnoreCase("Draw no Bet for fourth period")
                || market.name.EqualsIgnoreCase("Draw no Bet for second period")
                || market.name.EqualsIgnoreCase("Draw No Bet for third period")
                || market.name.EqualsIgnoreCase("2nd Half - Draw No Bet")
                || market.name.EqualsIgnoreCase("2nd Half - Draw no Bet, including overtime")
                )
            {
                return "W" + runner.name[0];
            }
            else if (


                   market.name.EqualsIgnoreCase("Total")
                || market.name.EqualsIgnoreCase("Total for whole match, including overtime")
                || market.name.EqualsIgnoreCase("Halftime: Total")
                || market.name.EqualsIgnoreCase("2nd Half: Total")
                || market.name.EqualsIgnoreCase("2nd Half - Total, including overtime")
                || market.name.EqualsIgnoreCase("Total for first period")
                || market.name.EqualsIgnoreCase("Total for second period")
                || market.name.EqualsIgnoreCase("Total for third period")
                || market.name.EqualsIgnoreCase("Total for fourth period")
                || market.name.EqualsIgnoreCase("Total for fifth period")
                || market.name.EqualsIgnoreCase("Total for sixth period")
                || market.name.EqualsIgnoreCase("Total for seventh period")
                || market.name.EqualsIgnoreCase("Total for eighth period")
                || market.name.EqualsIgnoreCase("Asian total")
                || market.name.EqualsIgnoreCase("Asian total first half")
                || market.name.EqualsIgnoreCase("Asian total for the whole match, including overtime")
                )

            {
                if (runner.name.StartsWithIgnoreCase("under")) return "TOTALUNDER";
                if (runner.name.StartsWithIgnoreCase("over")) return "TOTALOVER";
            }

            return string.Empty;
        }

        private void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            Lines.Add(lineDto);
        }
    }
}





