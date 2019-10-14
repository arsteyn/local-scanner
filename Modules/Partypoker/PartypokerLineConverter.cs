using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
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

            Parallel.ForEach(fvr.fixture.games, game =>
            {
                ConvertGame(fvr.fixture, game);
            });

            return Lines.ToArray();
        }


        private Dictionary<string, string> _simpleMap;

        static object _lock = new object();

        private void ConvertGame(Fixture fvrFixture, Game game)
        {
            //lock (_lock)
            //{
            //    var lines = File.ReadLines(@"C:\temp\names.txt").ToList();

            //    if (lines.All(l => l != $"case \"{game.name.value.ToLower()}\":"))
            //    {
            //        //рынок
            //        //добавляем линию
            //        lines.Add($"case \"{game.name.value.ToLower()}\":");
            //    }

            //    lines.Sort();

            //    File.WriteAllLines(@"C:\temp\names.txt", lines);


            //    foreach (var gameResult in game.results)
            //    {
            //        var resulta = File.ReadLines(@"C:\temp\results.txt").ToList();


            //        if (resulta.All(l => l != "case \"" + gameResult.name.value.ToLower() + "\":" + JsonConvert.SerializeObject(gameResult).ToString()))
            //        {
            //            //рынок
            //            //добавляем линию
            //            resulta.Add("case \"" + game.name.value.ToLower() + "\":" + JsonConvert.SerializeObject(gameResult));
            //        }

            //        resulta.Sort();

            //        File.WriteAllLines(@"C:\temp\results.txt", resulta);
            //    }

            //}


            if (!game.visibility.EqualsIgnoreCase("visible")) return;

            switch (game.name.value.ToLower())
            {

                case "match result":
                case "double chance":
                case "half time double chance":
                case "half time result":

                case "double chance (regular time)":
                case "double chance 1st period":
                case "double chance 2nd period":
                case "double chance 3rd period":

                    ConvertMainBets(fvrFixture, game);
                    break;

                case "draw no bet":

                    ConvertDnb(fvrFixture, game);

                    break;

                case "total goals - over/under":
                case "total goals o/u - 1st half":
                case "total goals o/u - 2nd half":
                case "totals (regular time)":
                case "totals":
                case "1st half totals":
                case "2nd half totals":

                //case "1st quarter totals (only points scored in this quarter)":
                //case "2nd quarter totals (only points scored in this quarter)":
                //case "3rd quarter totals (only points scored in this quarter)":
                //case "4th quarter totals (only points scored in this quarter)":
                case "1st period totals":
                case "2nd period totals":
                case "3rd period totals":

                    ConvertTotal(fvrFixture, game);
                    break;

                case "total goals o/u - team 1":

                    ConvertIndividualTotal(fvrFixture, game, 1);
                    break;
                case "total goals o/u - team 2":

                    ConvertIndividualTotal(fvrFixture, game, 2);
                    break;

                    //Handicap только европейские

            }
        }

        private void ConvertTotal(Fixture fvrFixture, Game game)
        {
            Parallel.ForEach(game.results, result =>
            {
                if (!result.visibility.EqualsIgnoreCase("visible")) return;

                var line = _lineTemplate.Clone();

                line.CoeffValue = result.odds;

                line.CoeffKind = "TOTAL" + result.name.value.Split(' ')[0].ToUpper();

                line.CoeffParam = decimal.Parse(result.name.value.Split(' ')[1].Replace(",", "."), CultureInfo.InvariantCulture);

                line.CoeffType = GetCoeffType(game);

                line.LineObject = fvrFixture.id + "|" + game.id + "|" + result.id;

                line.LineData = new BuyBetData(fvrFixture, game,result);

                AddLine(line);
            });
        }

        private void ConvertIndividualTotal(Fixture fvrFixture, Game game, int teamNumber)
        {

            Parallel.ForEach(game.results, result =>
            {
                if (!result.visibility.EqualsIgnoreCase("visible")) return;

                var line = _lineTemplate.Clone();

                line.CoeffValue = result.odds;

                line.CoeffKind = "ITOTAL" + result.name.value.Split(' ')[0].ToUpper() + teamNumber;

                line.CoeffParam = decimal.Parse(result.name.value.Split(' ')[1].Replace(",", "."), CultureInfo.InvariantCulture);

                line.CoeffType = GetCoeffType(game);

                line.LineObject = fvrFixture.id + "|" + game.id + "|" + result.id;

                line.LineData = new BuyBetData(fvrFixture, game, result);

                AddLine(line);
            });

        }

        private void ConvertMainBets(Fixture fvrFixture, Game game)
        {
            Parallel.ForEach(game.results, result =>
            {
                if (!result.visibility.EqualsIgnoreCase("visible")) return;

                var line = _lineTemplate.Clone();

                line.CoeffValue = result.odds;

                if (!_simpleMap.ContainsKey(result.name.value)) return;

                line.CoeffType = GetCoeffType(game);

                line.CoeffKind = _simpleMap[result.name.value];

                line.LineObject = fvrFixture.id + "|" + game.id + "|" + result.id;

                line.LineData = new BuyBetData(fvrFixture, game, result);

                AddLine(line);
            });
        }

        private void ConvertDnb(Fixture fvrFixture, Game game)
        {
            Parallel.ForEach(game.results, result =>
            {
                if (!result.visibility.EqualsIgnoreCase("visible")) return;

                var line = _lineTemplate.Clone();

                line.CoeffValue = result.odds;

                if (!_simpleMap.ContainsKey(result.name.value)) return;

                line.CoeffType = GetCoeffType(game);

                line.CoeffKind = "W" + _simpleMap[result.name.value];

                line.LineObject = fvrFixture.id + "|" + game.id + "|" + result.id;

                line.LineData = new BuyBetData(fvrFixture, game, result);

                AddLine(line);
            });

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
            lock (_lock) Lines.Add(lineDto);
        }

        
    }
}





