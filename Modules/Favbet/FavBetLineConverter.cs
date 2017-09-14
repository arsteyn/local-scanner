
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bars.EAS.Utils;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using Favbet.Models;
using Favbet.Models.Line;

namespace Favbet
{
    public class FavBetConverter
    {
        public readonly Regex TeamRegex = new Regex(@"^(?<home>.*?) - (?<away>.*?)$");
        public readonly Regex ScoreRegex = new Regex(@"(?<score1>\d+):(?<score2>\d+)(?:.*?[(].*?(?<pscore1>\d+):(?<pscore2>\d+)[)])?(?: \d+')?");

        public static Dictionary<string, string> MapRuls = new Dictionary<string, string>
        {
            // {Искомая_строка_1@Искомая_строка_2@ ... @Искомая_строка_n@Регулярка_для_обработки}, {ставки}.
            {@"money line", "1|X|2"},
            {@"match winner@draw no bet@2-way odds@set winner", "W1|W2"},
            {@"double chance", "1X|12|X2"},
            {@"handicap", "HANDICAP1|PARAM|HANDICAP2"},
            {@"over/under", "TOTALOVER|PARAM|TOTALUNDER"},
            {@"team total@player total games", "ITOTALOVER|PARAM|ITOTALUNDER"},
            {@"odd / even", "ODD|EVEN"}
        };

        readonly List<string> _stopWords = new List<string> { "cross", "goal", "shot", "offside", "corner", "foul" };

        public LineDTO CreateLineWithoutEvents(Game game, string host, string name)
        {
            try
            {
                var isValidGame = game != null && (string.IsNullOrEmpty(game.Status) || game.Status == "inprogress");

                if (!isValidGame) return null;

                game.Sport = Helper.ConvertSport(game.Sport);

                if (_stopWords.Any(st => game.Player1.Contains(st)) || _stopWords.Any(st => game.Player2.Contains(st)))
                {
                    return null;
                }

                var line = new LineDTO
                {
                    BookmakerName = name,
                    SportKind = game.Sport,
                    Team1 = game.Player1,
                    Team2 = game.Player2,
                    Url = $"{host}en/live/#event={game.Id}",
                    Score1 = game.ScoreTeam1,
                    Score2 = game.ScoreTeam2,
                    CoeffType = game.CurrentPeriod.ToLower(),
                    Pscore1 = game.PeriodScoreTeam1,
                    Pscore2 = game.PeriodScoreTeam2,
                    EventDate = DateTime.Now,
                    LineObject = game.Id.ToString()
                };

                return line;
            }
            catch (Exception e)
            {
                // ignored
            }

            return null;
        }

        public List<LineDTO> AddEventsToLine(LineDTO tamplate, Game game, string tournamentName)
        {
            var localLines = new List<LineDTO>();

            // Извлекаем линии-ставок из header секции.
            if (game.HeadSectionBet != null)
            {
                game.HeadSectionBet.InitHeaderSection();

                var gameType = string.IsNullOrEmpty(game.GameType) ? ConverterHelper.GetGameType(game.HeadSectionBet.BetGroupName) : game.GameType;

                var lines = ExtractLinesFromSection(tamplate, game.HeadSectionBet.Section, game.Player1, gameType, tournamentName);

                localLines.AddRange(lines);
            }
            // Извлекаем линии-ставок из дугих секций.
            if (game.SectionBets != null)
            {
                localLines.AddRange(ExtractLinesFromSections(tamplate, game.SectionBets, game.Player1, game.GameType, tournamentName));
            }

            return localLines;
        }

        private static List<LineDTO> ExtractLinesFromSections(LineDTO tample, List<Section> sections, string player, string gameType, string tournamentName)
        {
            var sectionsLines = new List<LineDTO>();
            foreach (var section in sections)
            {
                sectionsLines.AddRange(ExtractLinesFromSection(tample, section, player, gameType, tournamentName));
            }
            return sectionsLines;
        }

        private static List<LineDTO> ExtractLinesFromSection(LineDTO tample, Section section, string player, string gameType, string tournamentName)
        {
            var groupLines = new List<LineDTO>();

            if (section.BetsGroup == null)
                return groupLines;

            // Извлекаем тип игры: угловые, (желтые, красные карты), картер, и т.д.
            var localGameType = gameType;

            if (string.IsNullOrEmpty(localGameType))
            {
                localGameType = ConverterHelper.GetGameType(section.Name);
            }

            foreach (var betGroup in section.BetsGroup) // Пример: Full Time, Set 1, Set 2
            {
                string coeffKinds;
                // Если ставка не востребованна, т.е попалось ненужная ставка - то:

                //исключаем индивидуальные тоталы в футболе
                if (tample.SportKind.ContainsAllIgnoreCase("football") && betGroup.Name.ContainsIgnoreCase("player"))
                    continue;

                //исключаем тоталы 3way
                if (betGroup.Name != null && betGroup.Name.ContainsIgnoreCase("3way") && (betGroup.Name.ContainsIgnoreCase("over") || betGroup.Name.ContainsIgnoreCase("under")))
                    continue;

                if (!IsDemandMark(betGroup.Name, out coeffKinds))
                    continue;

                //исключаем гандикапы по сетам и геймам
                if (betGroup.Name.ContainsAllIgnoreCase("sets handicap") || betGroup.Name.ContainsAllIgnoreCase("games handicap"))
                    continue;

                // Извлекаем тип игры: угловые, (желтые, красные карты), картер, и т.д.
                if (string.IsNullOrEmpty(localGameType))
                {
                    localGameType = ConverterHelper.GetGameType(betGroup.Name);
                }

                foreach (var bet in betGroup.Bets) // Тоталы, форы, и т.д
                {
                    if (bet.IsDisabled.Equals("yes")) // игнорировать ставки: зачеркнутые - их нельзя купить
                        continue;

                    bool hasParam = ConverterHelper.HasParam(coeffKinds);
                    string[] coeffKindsSplit = coeffKinds.Split('|');

                    // Если коффициентов ставки > ожидаемыйх
                    if (bet.Odds.Count > coeffKindsSplit.Length)
                        continue;

                    /* Если ожидаемых коэффициентов ставки > входящих:
                        * Например: хоккей имеет ставку  "W1|X|W2", 
                        * в то врямя как для тенниса ставки "W1|W2".
                        * Ставки содержащие PARAM тут не обрабатываются.
                        */
                    if (!coeffKinds.Contains("PARAM") &&
                        bet.Odds.Count < coeffKindsSplit.Length)
                    {
                        coeffKinds = coeffKinds.Replace("|X|", "|");
                        coeffKindsSplit = coeffKinds.Split('|');
                    }

                    // Номер команды
                    var teamNum = string.Empty;
                    if (coeffKinds.Contains("ITOTAL"))
                    {
                        var team = Regex.Match(betGroup.Name, @"(?i)total(?-i)\s*(?<match>.*)").Groups["match"].Value;

                        if (team.ContainsIgnoreCase(tample.Team1))
                            teamNum = "1";
                        else if (team.ContainsIgnoreCase(tample.Team2))
                            teamNum = "2";
                        else
                            continue;
                    }

                    var countCoeffKinds = bet.Odds.Count;
                    for (int i = 0; i < countCoeffKinds; ++i) // игнорировать ставки: спрятанные или coef == null
                    {
                        var odds = bet.Odds[i];
                        if (!odds.Value.HasValue || odds.IsHidden.Equals("no")) // игнорировать ставки: зачеркнутые, спрятанные, coef == null
                            break;

                        var copy = tample.Clone();

                        // Извлекаем тип игры: угловые, (желтые, красные карты), картер, и т.д.
                        if (string.IsNullOrEmpty(localGameType))
                        {
                            localGameType = ConverterHelper.GetGameType(bet.Odds[i].Name);
                        }

                        int paramIndex = coeffKindsSplit[i].Equals("PARAM") ? i + 1 : i;

                        string period;
                        //добавляем только линии с известными периодами (full time, 1st half и т.д.)
                        if (!ConverterHelper.GetPeriod(section.Name, out period)) continue;

                        var periodFromGroupName = ConverterHelper.GetPeriodFromGroupName(betGroup.Name);

                        //если в названии группы есть указание периода берем его оттуда
                        if (!string.IsNullOrEmpty(periodFromGroupName)) period = periodFromGroupName;

                        //Только у FavBet тоталы на баскетбол на последнюю четверть считаются с учетом овертайма, в отличии от других контор
                        if (copy.SportKind.ContainsAllIgnoreCase("basketball") && period == "4th quarter" && coeffKindsSplit[paramIndex].Contains("TOTAL")) continue;

                        copy.CoeffType = $"{period} {localGameType}";

                        copy.CoeffValue = (decimal)odds.Value;
                        copy.CoeffKind = coeffKindsSplit[paramIndex] + teamNum;
                        copy.Name = copy.CoeffKind;

                        //Добавляем возраст если играют молодежные команды
                        const string regex = @"U[1-2][0-9]";

                        if (Regex.IsMatch(tournamentName, regex))
                        {
                            var age = Regex.Match(tournamentName, regex).Value;
                            copy.Team1 += " " + age;
                            copy.Team2 += " " + age;
                        }

                        if (hasParam)
                        {
                            var value = bet.Odds[1].Value;
                            if (value != null)
                            {
                                var coeffParam = bet.Odds.Count == 3 ? value.Value : ConverterHelper.ExtractCoeffParam(odds.Name);
                                copy.CoeffParam = (decimal)coeffParam;
                            }
                        }

                        copy.LineData = new StoreBet { OutcomeId = odds.Id.ToInt(), EventId = copy.LineObject.ToInt() };

                        copy.UpdateName();
                        groupLines.Add(copy);
                    }
                }
            }

            return groupLines;
        }

        private static bool IsDemandMark(string name, out string coeffKindsSplit)
        {
            if (!string.IsNullOrEmpty(name)) return ConverterHelper.IsDemandMark(MapRuls, name.ToLower(), out coeffKindsSplit);

            coeffKindsSplit = string.Empty;
            return false;
        }

    }
}


