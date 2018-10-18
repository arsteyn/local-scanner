using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bars.EAS.Utils;
using Newtonsoft.Json;

namespace Favbet.Models.Line
{
    public class FrontendApiResponseWrapper<T>
    {
        public List<T> Result { get; set; }
    }

    public class Market
    {
        [JsonProperty(PropertyName = "event_id")]
        public long event_id { get; set; }

        [JsonProperty(PropertyName = "event_status_type")]
        public string event_status_type { get; set; }

        [JsonProperty(PropertyName = "market_has_param")]
        public bool? market_has_param { get; set; }

        [JsonProperty(PropertyName = "market_id")]
        public long market_id { get; set; }

        [JsonProperty(PropertyName = "market_name")]
        public string market_name { get; set; }

        [JsonProperty(PropertyName = "market_suspend")]
        public bool? market_suspend { get; set; }

        [JsonProperty(PropertyName = "result_type_name")] // Учесть состояние pause, finished
        public string result_type_name { get; set; }

        [JsonProperty(PropertyName = "result_type_short_name")]
        public string result_type_short_name { get; set; }

        [JsonProperty(PropertyName = "outcomes")]
        public List<Outcome> outcomes { get; set; }

        /// <summary>
        /// Инициализирует все свойства игры, которые возможно только инициализировать
        /// на текущий момент.
        /// </summary>
        /// <param name="teamRegex">Правило (регулярка) извлечения команд из строки</param>
        /// <param name="scoreRegex">Правило (регулярка) извлечения счета и периода из строки</param>
        /// <param name="tournamentName"></param>
        /// <returns>true - если все прошло успешно</returns>
        //public bool TryInitProperties(Regex teamRegex, Regex scoreRegex)
        //{
        //    try
        //    {
        //        if (Players == null) return false;

        //        GameType = ConverterHelper.GetGameType(Players);

        //        Players = Players.Replace($"({GameType})", string.Empty).Trim();

        //        var teamMatch = teamRegex.Match(Players);
        //        Player1 = teamMatch.Groups["home"].Value;
        //        Player2 = teamMatch.Groups["away"].Value;

        //        // Извлекаем счет периода
        //        var scoreMatch = scoreRegex.Match(CurrentScore);

        //        if (scoreMatch.Success)
        //        {
        //            ScoreTeam1 = scoreMatch.Groups["score1"].Value.ToInt();
        //            ScoreTeam2 = scoreMatch.Groups["score2"].Value.ToInt();
        //            PeriodScoreTeam1 = scoreMatch.Groups["pscore1"].Value.ToIntNullable();
        //            PeriodScoreTeam2 = scoreMatch.Groups["pscore2"].Value.ToIntNullable();
        //        }

        //        return true;

        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        //throw;

        //        return false;
        //    }
        //}
    }

}
