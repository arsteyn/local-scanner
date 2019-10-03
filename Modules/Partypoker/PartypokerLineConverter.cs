using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Bars.EAS.Utils;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using Newtonsoft.Json;
using Partypoker.JsonClasses;

namespace Partypoker
{
    public class PartypokerLineConverter
    {
        public List<LineDTO> Lines;

        private LineDTO _lineTemplate;

        public LineDTO[] Convert(string bookmakerName, string value)
        {
            Lines = new List<LineDTO>();

            if (string.IsNullOrEmpty(value))
                return new LineDTO[] { };

            var fvr = JsonConvert.DeserializeObject<FixtureViewResponce>(value);

            if (fvr == null)
                return new LineDTO[] { };

            //событие не началось
            if (!fvr.fixture.stage.ContainsAllIgnoreCase("live"))
                return new LineDTO[] { };

            Lines = new List<LineDTO>();

            _lineTemplate = new LineDTO
            {
                BookmakerName = bookmakerName,
                Team1 = fvr.fixture.participants[0].name.value,
                Team2 = fvr.fixture.participants[1].name.value,
                SportKind = Helper.ConvertSport(fvr.fixture.sport.name.value),
                Score1 = fvr.fixture.scoreboard.score.Split(":")[0].ToInt(),
                Score2 = fvr.fixture.scoreboard.score.Split(":")[1].ToInt()
            };

            _simpleMap = new Dictionary<string, string>
            {
                {fvr.fixture.participants[0].name.value, "1"},
                {fvr.fixture.participants[1].name.value, "2"},
                {"X", "X"},
                {$"{fvr.fixture.participants[0].name.value} or X", "1X"},
                {$"X or {fvr.fixture.participants[1].name.value}", "X2"},
                {$"{fvr.fixture.participants[0].name.value} or {fvr.fixture.participants[0].name.value}", "12"}
            };

            foreach (var game in fvr.fixture.games)
                ConvertGame(game);

            return Lines.ToArray();
        }


        private Dictionary<string, string> _simpleMap;

        static object _lock = new object();

        private void ConvertGame(Game game)
        {
            lock (_lock)
            {
                var lines = File.ReadLines(@"C:\temp\names.txt").ToList();

                if (lines.All(l => l != "case \"" + game.name.value.ToLower() + "\":"))
                {
                    //добавляем линию
                    lines.Add("case \"" + game.name.value.ToLower() + "\":");
                }

                File.WriteAllLines(@"C:\temp\names.txt", lines);
            }


            if (!game.visibility.EqualsIgnoreCase("visible")) return;

            switch (game.name.value.ToLower())
            {

                case "match result":
                case "match winner":
                //case "who will win the 1st period?":
                //case "who will win the 2nd period?":
                //case "who will win the 3rd period?":

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

                    ConvertMainBets(game);
                    break;

                //case "total goals - over/under":
                //case "total goals o/u - 1st half":
                //case "total goals o/u - 2nd half":
                ////case "totals (regular time)":
                //case "totals":
                //case "1st half totals":
                //case "2nd half totals":

                //case "1st quarter totals (only points scored in this quarter)":
                //case "2nd quarter totals (only points scored in this quarter)":
                //case "3rd quarter totals (only points scored in this quarter)":
                //case "4th quarter totals (only points scored in this quarter)":
                //case "1st period totals":
                //case "2nd period totals":
                //case "3rd period totals":

                //    ConvertTotal(game.Results, game);
                //    break;

                //case "total goals o/u - team 1":
                //case "how many goals will team 1 score in 1st period?":
                //case "how many goals will team 1 score in 2nd period?":
                //case "how many goals will team 1 score in 3rd period?":
                //case "how many goals will team 1 score? (regular time)":

                //    ConvertIndividualTotal(game.Results, game, 1);
                //    break;

                //case "total goals o/u - team 2":
                //case "how many goals will team 2 score in 1st period?":
                //case "how many goals will team 2 score in 2nd period?":
                //case "how many goals will team 2 score in 3rd period?":
                //case "how many goals will team 2 score? (regular time)":

                //    ConvertIndividualTotal(game.Results, game, 2);
                //    break;

                //case "handicap (regular time)":
                //case "handicap":

                //case "1st quarter handicap (points scored in this quarter only)":
                //case "2nd quarter handicap (points scored in this quarter only)":
                //case "3rd quarter handicap (points scored in this quarter only)":
                //case "4th quarter handicap (points scored in this quarter only)":

                //case "1st half handicap":
                //case "2nd half handicap":

                ////case "3 way handicap (regular time)":
                ////case "3 way handicap (regular time) -1/+1":
                ////case "3 way handicap (regular time) -2/+2":
                ////case "3 way handicap (regular time) -3/+3":
                ////case "3 way handicap (regular time) -4/+4":
                ////case "3 way handicap (regular time) -5/+5":
                ////case "3 way handicap (regular time) -6/+6":
                ////case "3 way handicap (regular time) -7/+7":

                //case "1st period - 3 way handicap (only goals scored in this period) -1/+1":
                //case "2nd period - 3 way handicap (only goals scored in this period) -1/+1":
                //case "3rd period - 3 way handicap (only goals scored in this period) -1/+1":
                //case "4th period - 3 way handicap (only goals scored in this period) -1/+1":

                //    ConvertHandicap(game.Results, game);
                //    break;

            }
        }

        //private void ConvertHandicap(Game game)
        //{
        //    foreach (var result in game.results)
        //    {
        //        if (!result.Visible) continue;

        //        var line = _lineTemplate.Clone();

        //        line.CoeffValue = result.odds;

        //        if (result.Name.ContainsIgnoreCase(line.Team1))
        //            line.CoeffKind = "HANDICAP1";
        //        else if (result.Name.ContainsIgnoreCase(line.Team2))
        //            line.CoeffKind = "HANDICAP2";
        //        else
        //            continue;

        //        line.CoeffParam = decimal.Parse(result.Name.Split(' ').Last().Replace(",", "."), CultureInfo.InvariantCulture);

        //        line.CoeffType = GetCoeffType(market);

        //        line.LineObject = result.Self;

        //        AddLine(line);
        //    }
        //}

        //private static void ConvertTotal(Game game)
        //{
        //    foreach (var result in results)
        //    {
        //        if (!result.Visible) continue;

        //        if (!result.Name.ContainsIgnoreCase("over", "under")) continue;

        //        var line = _lineTemplate.Clone();

        //        line.CoeffValue = result.Odds;

        //        line.CoeffKind = "TOTAL" + result.Name.Split(' ')[0].ToUpper();

        //        line.CoeffParam = decimal.Parse(result.Name.Split(' ').Last().Replace(",", "."), CultureInfo.InvariantCulture);

        //        line.CoeffType = GetCoeffType(market);

        //        line.LineObject = result.Self;

        //        AddLine(line);
        //    }
        //}

        //private static void ConvertIndividualTotal(List<Result> results, Market market, int teamNumber)
        //{
        //    foreach (var result in results)
        //    {
        //        if (!result.Visible) continue;

        //        if (!result.Name.ContainsIgnoreCase("over", "under")) continue;

        //        var line = _lineTemplate.Clone();

        //        line.CoeffValue = result.Odds;

        //        line.CoeffKind = "ITOTAL" + result.Name.Split(' ')[0].ToUpper() + teamNumber;

        //        line.CoeffParam = decimal.Parse(result.Name.Split(' ').Last().Replace(",", "."), CultureInfo.InvariantCulture);

        //        line.CoeffType = GetCoeffType(market);

        //        line.LineObject = result.Self;

        //        AddLine(line);
        //    }
        //}

        private void ConvertMainBets(Game game)
        {
            foreach (var result in game.results)
            {
                if (!result.visibility.EqualsIgnoreCase("visible")) continue;

                var line = _lineTemplate.Clone();

                line.CoeffValue = result.odds;

                if (!_simpleMap.ContainsKey(result.name.value)) continue;

                line.CoeffType = GetCoeffType(game);

                line.CoeffKind = _simpleMap[result.name.value];

                line.LineObject = JsonConvert.SerializeObject(result);

                AddLine(line);
            }
        }

        private static string GetCoeffType(Game game)
        {
            var result = string.Empty;

            if (game.name.value.ContainsIgnoreCase("1st period")) result = "1st period";
            if (game.name.value.ContainsIgnoreCase("2nd period")) result = "2nd period";
            if (game.name.value.ContainsIgnoreCase("3rd period")) result = "3rd period";
            if (game.name.value.ContainsIgnoreCase("1st half")) result = "1st half";
            if (game.name.value.ContainsIgnoreCase("half time")) result = "1st half";
            if (game.name.value.ContainsIgnoreCase("2nd half")) result = "2nd half";
            if (game.name.value.ContainsIgnoreCase("1st quarter")) result = "1st quarter";
            if (game.name.value.ContainsIgnoreCase("2nd quarter")) result = "2nd quarter";
            if (game.name.value.ContainsIgnoreCase("3rd quarter")) result = "3rd quarter";
            if (game.name.value.ContainsIgnoreCase("4th quarter")) result = "4th quarter";

            return result;
        }


        private void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            Lines.Add(lineDto);
        }
    }
}





