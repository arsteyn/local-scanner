using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using JsonClasses;
using Leonbets.JsonClasses;
using Newtonsoft.Json;
using Scanner;

namespace Leonbets
{
    public class LeonBetsLineConverter
    {
        private static readonly Regex TeamRegex = new Regex("^(?<home>.*?) - (?<away>.*?)$");
        private static readonly Regex ScoreRegex = new Regex("^(?<homeScore>.*?) : (?<awayScore>.*?)$");

        public static List<LineDTO> Lines;


        internal static readonly List<string> StopWords = new List<string>
        {
            "totals", "number", "corner", "card", "booking", "penal"
        };

        //Convert single event
        public static LineDTO[] Convert(string response, string bookmakerName, Event @event)
        {
            Lines = new List<LineDTO>();

            var odds = JsonConvert.DeserializeObject<List<Odd>>(response);

            var teamMatch = TeamRegex.Match(@event.name);
            var teamScore = ScoreRegex.Match(@event.score);

            var lineTemplate = new LineDTO
            {
                Team1 = teamMatch.Groups["home"].Value,
                Team2 = teamMatch.Groups["away"].Value,
                BookmakerName = bookmakerName,
                Score1 = System.Convert.ToInt32(teamScore.Groups["homeScore"].Value),
                Score2 = System.Convert.ToInt32(teamScore.Groups["awayScore"].Value),
                SportKind = Helper.ConvertSport(@event.sName),
                EventDate = TimeExt.FromUnixTime(@event.kickoffDate)
            };

            foreach (var odd in odds)
            {
                //исключаем угловые
                if (StopWords.Any(s => odd.name.ContainsIgnoreCase(s))) continue;

                foreach (var runner in odd.runners)
                {
                    Convert(@event, odd, runner, lineTemplate, response);
                }
            }
            return Lines.ToArray();
        }

        private static void Convert(Event @event, Odd odd, Runner runner, LineDTO lineTemplate, string resp)
        {
            var line = lineTemplate.Clone();

            var kind = GetCoeffKind(line.SportKind, runner, odd);

            if (string.IsNullOrEmpty(kind)) return;

            line.CoeffKind = kind;

            decimal coeffValue;

            decimal.TryParse(runner.oddValue, NumberStyles.Any, CultureInfo.InvariantCulture, out coeffValue);

            line.CoeffValue = coeffValue;

            //в runner.name приходит значение иногда с минусом иногда с плюсом
            //if (runner.name.Contains("(") && runner.name.Contains(")"))
            if (!string.IsNullOrWhiteSpace(odd.specialOddsValue))
            {
                decimal coeffParam;
                //decimal.TryParse(runner.name.Split('(', ')')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out coeffParam);
                decimal.TryParse(odd.specialOddsValue.Split('(', ')')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out coeffParam);
                line.CoeffParam = coeffParam;

                if (line.CoeffKind == "HANDICAP2")
                    line.CoeffParam = line.CoeffParam * (-1);
            }

            line.CoeffType = GetCoeffType(line.SportKind, odd);

            line.LineData = new
            {
                matchId = @event.Id,
                odd = coeffValue,
                oddsTypeId = odd.id,
                outcome = runner.aid,
            };

            //line.LineObject = odd.specialOddsValue;

            AddLine(line);
        }

        private static string GetCoeffType(string sportKind, Odd odd)
        {
            var coeffType = string.Empty;

            if (odd.name.ContainsIgnoreCase("first half") || odd.name.ContainsIgnoreCase("halftime"))
            {
                return "1st half";
            }

            if (odd.name.ContainsIgnoreCase("2nd half") || odd.name.ContainsIgnoreCase("second half"))
            {
                return "2nd half";
            }

            if (!odd.name.Contains("period")) return coeffType;

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

            Helper.ReplaceCoeffTypes(odd.name, out period);

            coeffType = $"{period} {periodType}";

            return coeffType;
        }

        private static string GetCoeffKind(string sportKind, Runner runner, Odd odd)
        {
            if (sportKind == "Basketball")
            {
                if (odd.name == "Odd/Even" || odd.name == "1X2") return string.Empty;
                if (odd.name.StartsWith("Asian", StringComparison.OrdinalIgnoreCase)) return string.Empty;
                if (odd.name == "Total") return string.Empty;
            }

            if (odd.name.StartsWithIgnoreCase("1X2")
                ||
                odd.name.StartsWithIgnoreCase("Halftime: 3way")
                ||
                odd.name.StartsWithIgnoreCase("2nd Half: 3way")
                ||
                odd.name.StartsWithIgnoreCase("Double chance (1X:12:X2)")
                ||
                odd.name.StartsWithIgnoreCase("Odd/Even")
                )
            {
                return runner.aid.ToUpper();
            }
            //142 - Total hometeam 
            if (odd.oddsType == 142)
            {
                if (runner.aid.StartsWithIgnoreCase("u")) return "ITOTALUNDER1";
                if (runner.aid.StartsWithIgnoreCase("o")) return "ITOTALOVER1";
            }
            //143 - Total awayteam 
            else if (odd.oddsType == 143)
            {
                if (runner.aid.StartsWithIgnoreCase("u")) return "ITOTALUNDER2";
                if (runner.aid.StartsWithIgnoreCase("o")) return "ITOTALOVER2";
            }
            else if (odd.name.StartsWithIgnoreCase("Asian"))
            {
                if (odd.name.ContainsIgnoreCase("handicap")) return "HANDICAP" + runner.aid;

                if (odd.name.ContainsIgnoreCase("total"))
                {
                    if (runner.aid.StartsWithIgnoreCase("u")) return "TOTALUNDER";
                    if (runner.aid.StartsWithIgnoreCase("o")) return "TOTALOVER";
                }
            }
            else if (odd.name.StartsWithIgnoreCase("Draw no bet"))
            {
                return "W" + runner.aid;
            }
            else if ((odd.name.StartsWithIgnoreCase("Total")))
            {
                if (runner.aid.StartsWithIgnoreCase("u")) return "TOTALUNDER";
                if (runner.aid.StartsWithIgnoreCase("o")) return "TOTALOVER";
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





