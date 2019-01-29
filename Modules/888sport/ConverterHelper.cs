using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Bars.EAS.Utils.Extension;
using BM.Web;

using Newtonsoft.Json;
using NLog;
using S888.Models.Line;
using Scanner.Helper;


namespace S888
{
    public class ConverterHelper
    {
        static Logger Log => LogManager.GetCurrentClassLogger();

        public static EventFull GetFullLine(long id, WebProxy proxy, string host)
        {
            EventFull e = null;
            try
            {
                using (var wc = new GetWebClient(proxy))
                {
                    var response = wc.DownloadString($"{host}offering/v2018/888/betoffer/event/{id}.json?lang=en_GB&market=En");
                    e = JsonConvert.DeserializeObject<EventFull>(response);
                }
            }
            catch (Exception exception)
            {
                Log.Info("S888 GetFullLine exception " + JsonConvert.SerializeObject(exception));
            }

            //foreach (var offer in e.BetOffers)
            //{
            //    foreach (var outcome in offer.outcomes)
            //    {
            //        ProxyHelper.Update888Events(e.Event[0].sport + " | " + offer.criterion.englishLabel + " | " + offer.betOfferType.englishName + " | " + outcome.englishLabel + " | "+ outcome.line);
            //    }
            //}

            return e;
        }

        public static Dictionary<string, string> PeriodMap = new Dictionary<string, string>
        {
            {"full time", ""},
            {"including overtime", "with et"},
            {"half time", "1st half"},
            {"1st half", "1st half"},
            {"2nd half", "2nd half"},
            {"1-st half", "1st half"},
            {"2-nd half", "2nd half"},

            {"quarter 1", "1st quarter"},
            {"quarter 2", "2nd quarter"},
            {"quarter 3", "3rd quarter"},
            {"quarter 4", "4th quarter"},

            {"period 1", "1st period"},
            {"period 2", "2nd period"},
            {"period 3", "3rd period"},
            {"period 4", "4th period"},

            {"inning 1", "1st inning"},
            {"inning 2", "2nd inning"},
            {"inning 3", "3rd inning"},
            {"inning 4", "4th inning"},

            {"set 1", "1st set"},
            {"1st Set", "1st set"},

            {"set 2", "2nd set"},
            {"2nd Set", "2nd set"},

            {"set 3", "3rd set"},
            {"3rd Set", "3rd set"},

            {"set 4", "4th set"},
            {"4th Set", "4th set"},

            {"set 5", "5th set"},
            {"5th Set", "5th set"},

            {"round 1", "1st round"},
            {"round 2", "2nd round"},
            {"round 3", "3rd round"},
            {"round 4", "4th round"},
            {"round 5", "5th round"},
            {"round 6", "6th round"},
            {"round 7", "7th round"},
            {"round 8", "8th round"},
            {"round 9", "9th round"},
            {"round 10", "10th round"},
            {"round 11", "11th round"}
        };

        public static string GetPeriod(string value)
        {
            value = value.ToLower();

            if (!PeriodMap.Keys.Any(k => value.Contains(k))) return string.Empty;

            var f = PeriodMap.Keys.First(k => value.Contains(k));

            return PeriodMap[f];
        }

    }
}
