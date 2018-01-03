using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using Bwin.JsonClasses;
using Newtonsoft.Json;

namespace Bwin
{
    public class BwinLineConverter
    {
        public static List<LineDTO> Lines;
        private static LineDTO _lineTemplate;

        public static LineDTO[] Convert(string bookmakerName, string value)
        {
            Lines = new List<LineDTO>();


            if (string.IsNullOrEmpty(value))
                return new LineDTO[] { };

            var @event = JsonConvert.DeserializeObject<EventResponce>(value).@event;

            if (@event == null)
                return new LineDTO[] { };

            //событие не началось
            if (@event.Scoreboard.Score == null || !@event.Scoreboard.Score.Team1.Counters.Any())
                return new LineDTO[] { };

            Lines = new List<LineDTO>();

            _lineTemplate = new LineDTO
            {
                BookmakerName = bookmakerName,
                Team1 = @event.Player1,
                Team2 = @event.Player2,
                SportKind = Helper.ConvertSport(@event.Sport.Name),
                Score1 = @event.Scoreboard.Score.Team1.Counters.OrderByDescending(c => c.PeriodId).First().Value,
                Score2 = @event.Scoreboard.Score.Team2.Counters.OrderByDescending(c => c.PeriodId).First().Value
            };

            Convert(@event);

            return Lines.ToArray();
        }


        private static Dictionary<string, string> _simpleMap;

        private static void Convert(Event @event)
        {

            _simpleMap = new Dictionary<string, string>
            {
                {@event.Player1, "1"},
                {@event.Player2, "2"},
                {"X", "X"},
                {$"{@event.Player1} or X", "1X"},
                {$"{@event.Player2} or X", "X2"}
            };


            foreach (var eventMarket in @event.Markets)
            {
                Convert(eventMarket);
            }

        }

        private static void Convert(Market market)
        {
            //var lines = File.ReadLines(@"C:\temp\names.txt").ToList();

            //if (lines.All(l => l != "case \"" + market.Name.ToLower() + "\":"))
            //{
            //    //добавляем линию
            //    lines.Add("case \"" + market.Name.ToLower() + "\":");
            //}

            //File.WriteAllLines(@"C:\temp\names.txt", lines);

            switch (market.Name.ToLower())
            {

                case "match result":
                case "match winner":
                case "who will win the 1st period?":
                case "who will win the 2nd period?":
                case "who will win the 3rd period?":

                case "double chance (regular time)":
                case "double chance 1st period":
                case "double chance 2nd period":
                case "double chance 3rd period":
                case "half time double chance":

                case "money line":
                case "moneyline":

                case "1st half money line":
                case "2nd half money line":

                case "1st quarter moneyline":
                case "2nd quarter moneyline":
                case "3rd quarter moneyline":
                case "4th quarter moneyline":

                    ConvertMainBets(market.Results, market);
                    break;

                case "total goals - over/under":
                case "total goals o/u - 1st half":
                case "total goals o/u - 2nd half":
                //case "totals (regular time)":
                case "totals":
                case "1st half totals":
                case "2nd half totals":

                case "1st quarter totals (only points scored in this quarter)":
                case "2nd quarter totals (only points scored in this quarter)":
                case "3rd quarter totals (only points scored in this quarter)":
                case "4th quarter totals (only points scored in this quarter)":
                case "1st period totals":
                case "2nd period totals":
                case "3rd period totals":

                    ConvertTotal(market.Results, market);
                    break;

                case "total goals o/u - team 1":
                case "how many goals will team 1 score in 1st period?":
                case "how many goals will team 1 score in 2nd period?":
                case "how many goals will team 1 score in 3rd period?":
                case "how many goals will Team 1 score? (regular time)":

                    ConvertIndividualTotal(market.Results, market, 1);
                    break;

                case "total goals o/u - team 2":
                case "how many goals will team 2 score in 1st period?":
                case "how many goals will team 2 score in 2nd period?":
                case "how many goals will team 2 score in 3rd period?":
                case "how many goals will team 2 score? (regular time)":

                    ConvertIndividualTotal(market.Results, market, 2);
                    break;

                case "handicap (regular time)":
                case "handicap":

                case "1st quarter handicap (points scored in this quarter only)":
                case "2nd quarter handicap (points scored in this quarter only)":
                case "3rd quarter handicap (points scored in this quarter only)":
                case "4th quarter handicap (points scored in this quarter only)":

                case "1st half handicap":
                case "2nd half handicap":

                case "3 way handicap (regular time)":
                case "3 way handicap (regular time) -1/+1":
                case "3 way handicap (regular time) -2/+2":
                case "3 way handicap (regular time) -3/+3":
                case "3 way handicap (regular time) -4/+4":
                case "3 way handicap (regular time) -5/+5":
                case "3 way handicap (regular time) -6/+6":
                case "3 way handicap (regular time) -7/+7":

                case "1st period - 3 way handicap (only goals scored in this period) -1/+1":
                case "2nd period - 3 way handicap (only goals scored in this period) -1/+1":
                case "3rd period - 3 way handicap (only goals scored in this period) -1/+1":
                case "4th period - 3 way handicap (only goals scored in this period) -1/+1":

                    ConvertHandicap(market.Results, market);
                    break;

            }
        }

        private static void ConvertHandicap(List<Result> results, Market market)
        {
            foreach (var result in results)
            {
                if (!result.Visible) continue;

                var line = _lineTemplate.Clone();

                line.CoeffValue = result.Odds;

                if (result.Name.ContainsIgnoreCase(line.Team1))
                    line.CoeffKind = "HANDICAP1";
                else if (result.Name.ContainsIgnoreCase(line.Team2))
                    line.CoeffKind = "HANDICAP2";
                else
                    continue;

                line.CoeffParam = decimal.Parse(result.Name.Split(' ').Last().Replace(",", "."), CultureInfo.InvariantCulture);

                line.CoeffType = GetCoeffType(market);

                line.LineObject = result.Self;

                AddLine(line);
            }
        }

        private static void ConvertTotal(List<Result> results, Market market)
        {
            foreach (var result in results)
            {
                if (!result.Visible) continue;

                if (!result.Name.ContainsIgnoreCase("over", "under")) continue;

                var line = _lineTemplate.Clone();

                line.CoeffValue = result.Odds;

                line.CoeffKind = "TOTAL" + result.Name.Split(' ')[0].ToUpper();

                line.CoeffParam = decimal.Parse(result.Name.Split(' ').Last().Replace(",", "."), CultureInfo.InvariantCulture);

                line.CoeffType = GetCoeffType(market);

                line.LineObject = result.Self;

                AddLine(line);
            }
        }

        private static void ConvertIndividualTotal(List<Result> results, Market market, int teamNumber)
        {
            foreach (var result in results)
            {
                if (!result.Visible) continue;

                if (!result.Name.ContainsIgnoreCase("over", "under")) continue;

                var line = _lineTemplate.Clone();

                line.CoeffValue = result.Odds;

                line.CoeffKind = "ITOTAL" + result.Name.Split(' ')[0].ToUpper() + teamNumber;

                line.CoeffParam = decimal.Parse(result.Name.Split(' ').Last().Replace(",", "."), CultureInfo.InvariantCulture);

                line.CoeffType = GetCoeffType(market);

                line.LineObject = result.Self;

                AddLine(line);
            }
        }

        private static void ConvertMainBets(List<Result> results, Market market)
        {
            foreach (var result in results)
            {
                var line = _lineTemplate.Clone();

                line.CoeffValue = result.Odds;

                if (!_simpleMap.ContainsKey(result.Name)) continue;

                line.CoeffType = GetCoeffType(market);

                line.CoeffKind = _simpleMap[result.Name];

                line.LineObject = result.Self;

                AddLine(line);
            }
        }

        private static string GetCoeffType(Market market)
        {
            var result = string.Empty;

            if (market.Name.ContainsIgnoreCase("1st period")) result = "1st period";
            if (market.Name.ContainsIgnoreCase("2nd period")) result = "2nd period";
            if (market.Name.ContainsIgnoreCase("3rd period")) result = "3rd period";
            if (market.Name.ContainsIgnoreCase("1st half")) result = "1st half";
            if (market.Name.ContainsIgnoreCase("half time")) result = "1st half";
            if (market.Name.ContainsIgnoreCase("2nd half")) result = "2nd half";
            if (market.Name.ContainsIgnoreCase("1st quarter")) result = "1st quarter";
            if (market.Name.ContainsIgnoreCase("2nd quarter")) result = "2nd quarter";
            if (market.Name.ContainsIgnoreCase("3rd quarter")) result = "3rd quarter";
            if (market.Name.ContainsIgnoreCase("4th quarter")) result = "4th quarter";

            return result;
        }


        private static void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            Lines.Add(lineDto);
        }
    }
}





