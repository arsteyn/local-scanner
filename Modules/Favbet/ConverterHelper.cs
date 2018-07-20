using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using BM.Web;
using Favbet.Models.Line;
using Newtonsoft.Json;
using Scanner;

namespace Favbet
{
    public class ConverterHelper
    {
        public static Game GetFullGame(int id, WebProxy proxy, CookieContainer cont, string host)
        {
            using (var wc = new Extensions.WebClientEx(proxy, cont) { Headers = {["User-Agent"] = GetWebClient.DefaultUserAgent } })
            {
                var eventUri = new Uri(host + "live/markets/event/");

                var json = wc.DownloadString($"{eventUri}{id}/");

                var game = JsonConvert.DeserializeObject<Game>(json);

                return game?.Sport == null ? null : game;
            }
        }

        public static bool HasParam(string value)
        {
            return value.Contains("HANDICAP") ||
                   value.Contains("TOTAL") ||
                   value.Contains("PARAM");
        }

        public static bool IsDemandMark(Dictionary<string, string> map, string name,
            out string coeffKindsSplit)
        {
            coeffKindsSplit = string.Empty;

            foreach (var rule in map)
            {
                var keyWords = rule.Key.Split('@');
                if (!keyWords.Any(name.Contains)) continue;
                coeffKindsSplit = rule.Value;
                return true;
            }
            return false;
        }

        public static double ExtractCoeffParam(string name)
        {
            string value = Regex.Match(name,
                                @"(?<match>(\+|\-)?(?:\d*(\.|\,))?\d+)",
                                RegexOptions.RightToLeft).Value.Replace(",", ".");
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        public static Dictionary<string, string> PeriodMap = new Dictionary<string, string>
        {
            { "full time", "" },
            { "match (with et)", "with et"},
            { "1st half", "1st half" },
            { "2nd half", "2nd half" },
            { "1-st half", "1st half" },
            { "2-nd half", "2nd half" },

            { "quarter 1", "1st quarter" },
            { "quarter 2", "2nd quarter" },
            { "quarter 3", "3rd quarter" },
            { "quarter 4", "4th quarter" },

            { "period 1", "1st period" },
            { "period 2", "2nd period" },
            { "period 3", "3rd period" },
            { "period 4", "4th period" },

            { "inning 1", "1st inning" },
            { "inning 2", "2nd inning" },
            { "inning 3", "3rd inning" },
            { "inning 4", "4th inning" },

            { "set 1", "1st set" },
            { "1st Set", "1st set" },

            { "set 2", "2nd set" },
            { "2nd Set", "2nd set" },

            { "set 3", "3rd set" },
            { "3rd Set", "3rd set" },

            { "set 4", "4th set" },
            { "4th Set", "4th set" },

            { "set 5", "5th set" },
            { "5th Set", "5th set" },

            { "round 1", "1st round" },
            { "round 2", "2nd round" },
            { "round 3", "3rd round" },
            { "round 4", "4th round" },
            { "round 5", "5th round" },
            { "round 6", "6th round" },
            { "round 7", "7th round" },
            { "round 8", "8th round" },
            { "round 9", "9th round" },
            { "round 10", "10th round" },
            { "round 11", "11th round" }
        };

        public static List<string> GameTypes = new List<string>
        {
            "corners",
            "yellow cards",
            "red cards",
            "qatar",
            "player"
        };

        public static bool GetPeriod(string value, out string period)
        {
            period = "";

            value = value.ToLower();

            if (!PeriodMap.ContainsKey(value)) return false;

            period = PeriodMap[value];

            return true;
        }

        public static string GetPeriodFromGroupName(string groupName)
        {
            groupName = groupName.ToLower();

            foreach (var map in PeriodMap.Where(map => groupName.Contains(map.Key)))
            {
                return map.Value;
            }

            return string.Empty;
        }

        public static string GetGameType(string header)
        {
            if (string.IsNullOrEmpty(header))
                return string.Empty;

            var localHeader = header.ToLower();
            var gameType = GameTypes.SingleOrDefault(t => localHeader.Contains(t));

            return gameType ?? string.Empty;
        }


    }
}
