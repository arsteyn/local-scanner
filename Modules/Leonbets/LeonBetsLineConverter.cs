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
using Scanner;
using Scanner.Helper;

namespace Leonbets
{
    public class LeonBetsLineConverter
    {
        private static readonly Regex TeamRegex = new Regex("^(?<home>.*?) - (?<away>.*?)$");
        private static readonly Regex ScoreRegex = new Regex("^(?<homeScore>.*?):(?<awayScore>.*?)$");

        public static List<LineDTO> Lines;

        internal static readonly List<string> StopWords = new List<string>
        {
            "totals", "number", "corner", "card", "booking", "penal"
        };

        //Convert single event
        public static LineDTO[] Convert(Event @event, string bookmakerName)
        {
            Lines = new List<LineDTO>();
            var teamMatch = TeamRegex.Match(@event.name);
            var teamScore = ScoreRegex.Match(@event.liveStatus.score);

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
                       
                    }
                }
            }
            return Lines.ToArray();
        }

        private static void Convert(Event @event, Market market, Runner runner, LineDTO lineTemplate)
        {
            var line = lineTemplate.Clone();

            var kind = GetCoeffKind(line.SportKind, runner, market);

            if (string.IsNullOrEmpty(kind)) return;

            line.CoeffKind = kind;

            decimal coeffValue;

            decimal.TryParse(runner.price, NumberStyles.Any, CultureInfo.InvariantCulture, out coeffValue);

            line.CoeffValue = coeffValue;

            //в runner.name приходит значение иногда с минусом иногда с плюсом
            if (runner.name.Contains("(") && runner.name.Contains(")")) { 
                //if (!string.IsNullOrWhiteSpace(market.specialOddsValue))
                //{
                    decimal coeffParam;
                    decimal.TryParse(runner.name.Split('(', ')')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out coeffParam);
                    //decimal.TryParse(market.name.Split('(', ')')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out coeffParam);
                    line.CoeffParam = coeffParam;

                    if (line.CoeffKind == "HANDICAP2")
                        line.CoeffParam = line.CoeffParam * (-1);
                //}
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
            var coeffType = string.Empty;

            if (market.name.ContainsIgnoreCase("first half") || market.name.ContainsIgnoreCase("halftime"))
            {
                return "1st half";
            }

            if (market.name.ContainsIgnoreCase("2nd half") || market.name.ContainsIgnoreCase("second half"))
            {
                return "2nd half";
            }

            if (!market.name.Contains("period")) return coeffType;

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

            string period;

            Helper.ReplaceCoeffTypes(market.name, out period);

            coeffType = $"{period} {periodType}";

            return coeffType;
        }

        private static string GetCoeffKind(string sportKind, Runner runner, Market market)
        {
            ProxyHelper.UpdateLeonEvents(market.name);

            if (sportKind == "Basketball")
            {
                if (market.name == "Odd/Even" || market.name == "1X2") return string.Empty;
                if (market.name.StartsWith("Asian", StringComparison.OrdinalIgnoreCase)) return string.Empty;
                if (market.name == "Total") return string.Empty;
            }

            if (market.name.StartsWithIgnoreCase("1X2")
                ||
                market.name.StartsWithIgnoreCase("Halftime: 3way")
                ||
                market.name.StartsWithIgnoreCase("2nd Half: 3way")
                ||
                market.name.StartsWithIgnoreCase("Double chance (1X:12:X2)")
                ||
                market.name.StartsWithIgnoreCase("Odd/Even")
                )
            {
                return runner.name.ToUpper();
            }
           
            if (market.name.EqualsIgnoreCase("Total hometeam"))
            {
                if (runner.name.StartsWithIgnoreCase("under")) return "ITOTALUNDER1";
                if (runner.name.StartsWithIgnoreCase("over")) return "ITOTALOVER1";
            }

            else if (market.name.EqualsIgnoreCase("Total awayteam"))
            {
                if (runner.name.StartsWithIgnoreCase("under")) return "ITOTALUNDER2";
                if (runner.name.StartsWithIgnoreCase("over")) return "ITOTALOVER2";
            }

            else if (market.name.StartsWithIgnoreCase("Asian"))
            {
                if (market.name.ContainsIgnoreCase("handicap")) return "HANDICAP" + runner.name[0];

                if (market.name.ContainsIgnoreCase("total"))
                {
                    if (runner.name.StartsWithIgnoreCase("under")) return "TOTALUNDER";
                    if (runner.name.StartsWithIgnoreCase("over")) return "TOTALOVER";
                }
            }
            else if (market.name.StartsWithIgnoreCase("Draw no bet"))
            {
                return "W" + runner.name[0];
            }
            else if ((market.name.Contains("Total")))
            {
                if (runner.name.StartsWithIgnoreCase("under")) return "TOTALUNDER";
                if (runner.name.StartsWithIgnoreCase("over")) return "TOTALOVER";
            }

            return string.Empty;
        }

        private static void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            Lines.Add(lineDto);
        }
    }
}





