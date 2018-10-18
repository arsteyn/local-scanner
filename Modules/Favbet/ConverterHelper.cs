using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BM.Web;
using Favbet.Models.Line;
using Newtonsoft.Json;
using Scanner.Helper;

namespace Favbet
{
    public class ConverterHelper
    {
        public static List<Market> GetMarketsByEvent(long id, WebProxy proxy, CookieContainer cont, string host)
        {
            using (var wc = new PostWebClient(proxy, cont.GetAllCookies()))
            {
                var query = $"{{\"jsonrpc\":\"2.0\",\"method\":\"frontend/market/get\",\"id\":{new Random().Next(100, 9999)},\"params\":{{\"by\":{{\"lang\":\"en\",\"service_id\":1,\"event_id\":{id}}}}}}}";

                var response = wc.UploadString($"{host}frontend_api2/", query);

                var t = JsonConvert.DeserializeObject<FrontendApiResponseWrapper<Market>>(response);

                //foreach (var market in t.Result)
                //{
                //    foreach (var outcome in market.outcomes)
                //    {
                //        ProxyHelper.UpdateFavbetEvents(market.market_name + " | " + market.result_type_name + " | " + outcome.outcome_name + " | " +outcome.outcome_param);
                //    }
                //}

                return t.Result;
            }
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

        public static string GetGameType(string header)
        {
            if (string.IsNullOrEmpty(header)) return string.Empty;

            var gameType = GameTypes.SingleOrDefault(t => header.ToLower().Contains(t));

            return gameType ?? string.Empty;
        }


    }
}
