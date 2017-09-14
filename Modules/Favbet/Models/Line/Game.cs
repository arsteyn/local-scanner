using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bars.EAS.Utils;
using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class Game
    {
        [JsonProperty(PropertyName = "event_id")]
        public int Id { get; set; }

        //[JsonProperty(PropertyName = "category_name")]
        public string GameType { get; set; }

        [JsonProperty(PropertyName = "event_name")]
        public string Players { get; set; } // команда 1 - команда 2

        [JsonProperty(PropertyName = "event_result_name")]
        public string CurrentPeriod { get; set; }

        [JsonProperty(PropertyName = "event_result_total")]
        public string CurrentScore { get; set; }

        [JsonProperty(PropertyName = "event_timer")] // Учесть состояние pause
        public Timer Timer { get; set; }

        [JsonProperty(PropertyName = "sport_is_timer")]
        public bool IsTimer { get; set; }

        [JsonProperty(PropertyName = "sport_name")] // Учесть состояние pause
        public string Sport { get; set; }

        [JsonProperty(PropertyName = "event_status_type")] // Учесть состояние pause, finished
        public string Status { get; set; }

        [JsonProperty(PropertyName = "market_count_active")]
        public int? MarketCount { get; set; }

        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public int ScoreTeam1 { get; set; }
        public int ScoreTeam2 { get; set; }
        public int? PeriodScoreTeam1 { get; set; }
        public int? PeriodScoreTeam2 { get; set; }

        [JsonProperty(PropertyName = "head_market")]
        public HeaderSection HeadSectionBet { get; set; }

        [JsonProperty(PropertyName = "result_types")]
        public List<Section> SectionBets { get; set; }

        /// <summary>
        /// Инициализирует все свойства игры, которые возможно только инициализировать
        /// на текущий момент.
        /// </summary>
        /// <param name="teamRegex">Правило (регулярка) извлечения команд из строки</param>
        /// <param name="scoreRegex">Правило (регулярка) извлечения счета и периода из строки</param>
        /// <param name="tournamentName"></param>
        /// <returns>true - если все прошло успешно</returns>
        public bool TryInitProperties(Regex teamRegex, Regex scoreRegex)
        {
            try
            {
                if (Players == null) return false;

                GameType = ConverterHelper.GetGameType(Players);

                Players = Players.Replace($"({GameType})", string.Empty).Trim();

                var teamMatch = teamRegex.Match(Players);
                Player1 = teamMatch.Groups["home"].Value;
                Player2 = teamMatch.Groups["away"].Value;

                // Извлекаем счет периода
                var scoreMatch = scoreRegex.Match(CurrentScore);

                if (scoreMatch.Success)
                {
                    ScoreTeam1 = scoreMatch.Groups["score1"].Value.ToInt();
                    ScoreTeam2 = scoreMatch.Groups["score2"].Value.ToInt();
                    PeriodScoreTeam1 = scoreMatch.Groups["pscore1"].Value.ToIntNullable();
                    PeriodScoreTeam2 = scoreMatch.Groups["pscore2"].Value.ToIntNullable();
                }

                return true;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;

                return false;
            }
        }
    }
}
