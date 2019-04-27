using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BM.Core;
using BM.DTO;
using BM.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scanner;
using Scanner.Helper;
using Extensions = Scanner.Extensions;

namespace Sbobet
{
    public class SbobetScanner : ScannerBase
    {
        public override string Name => "Sbobet";

        public override string Host => "https://www.dafabet.com/";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();

        public static readonly string[] LeagueStopWords = {
            "fantasy",
            "corner",
            "specific",
            "statistics",
            "crossbar",
            "goalpost",
            "fouls",
            "offsides",
            "shot",
            "booking",
            "penalty",
            "special",
            "goal",
            "kick",
            "offside",
            "throw",
            "over",
            "under",
            //penalty
            "(PEN)",
            //extra time
            "(ET)"
        };

        private const string BASE_URL = "https://ismart.dafabet.com/";
        private static readonly string GET_MARKETS_URL = $"{BASE_URL}Odds/GetMarket";
        private static readonly string GET_ALL_ODS_URL = $"{BASE_URL}Odds/ShowAllOdds";
        private static readonly string GET_CONTRIBUTOR_URL = $"{BASE_URL}main/GetContributor";

        readonly object _lock = new object();

        protected override void UpdateLiveLines()
        {
            var st = new Stopwatch();

            st.Start();

            try
            {
                var randomProxy = ProxyList.PickRandom();

                var randomProxy = CookieDictionary[randomProxy].GetData();

                var url = GetUrl(out var cookies);

                var response = Helper.WebGet(url, cookies);

                var match = Regex.Match(response, "onUpdate[(]'od',(?<json>.*?)[)];");
                var json = match.Groups["json"].Value;


                if (string.IsNullOrEmpty(json))
                {
                    CookieProvider.Delete(BmUser.Id);

                    return new LineDTO[] { };
                }

                SBHelper.Host = Host;
                SBHelper.Proxy = BmUser.GetWebProxy;

                var converter = container.Resolve<ILineConverter>("SbobetLineConverter");

                var lines = converter.Convert(json, Name);

                #region testbuy

                //var testLine = lines.OrderBy(x => x.CoeffValue).First(l => l.SportKind == "FOOTBALL");

                //testLine.Price = 3;

                //bool buySuccess;

                //if (Validate(testLine))
                //    buySuccess = BuyBet(testLine);

                //Clear(testLine);

                #endregion

                return lines;

                ActualLines = linesResult.ToArray();

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

            }
            catch (Exception e)
            {
                Log.Info($"ERROR Sbobet {e.Message} {e.StackTrace}");
            }
        }

        private string GetUrl(CookieContainer cookies)
        {
            var ts = cookies.GetAllCookies().GetValue(SBHelper.TS);

            return string.Format(SBHelper.GetAltHttp(cookies) + "/en/data/event?ts={0}&tk=204268,0,25,0,0,0,0,{1},1,4,0,4&ac=1", ts, DateTime.Now.ToString("yyyyMMdd"));
        }


        protected override void CheckDict()
        {
            var listToDelete = new List<WebProxy>();

            foreach (var account in _accounts)
            {
                foreach (var host in ProxyList)
                {
                    if (CookieDictionary.ContainsKey(host)) continue;

                    CookieDictionary.Add(host, new CachedArray<CookieContainer>(1000 * 60 * 15, () =>
                    {
                        try
                        {
                            var result = new CookieContainer();

                            result.Add(Authorize(host, account.Key, account.Value));

                            return result;
                        }
                        catch (Exception)
                        {
                            listToDelete.Add(host);

                            ConsoleExt.ConsoleWriteError($"Sbobet delete address {host.Address} listToDelete {listToDelete.Count}");
                        }

                        return null;
                    }));

                    if (CookieDictionary[host].GetData() != null) break;

                    CookieDictionary.Remove(host);
                }
            }

            foreach (var host in ProxyList.OrderBy(p => Guid.NewGuid()))
            {
                if (!CookieDictionary.ContainsKey(host)) ProxyList.Remove(host);
            }
        }

        private CookieCollection Authorize(WebProxy host, string login, string password)
        {
            var helper = new SBHelper(host);

            var url = helper.ProcessSignIn(login, password, Host, out var cookieCollection);

            var domain = new Uri(url).GetLeftPart(UriPartial.Authority);

            while (true)
            {
                var location = helper.GetLocation(url, ref cookieCollection, domain);

                if (string.IsNullOrEmpty(location)) break;

                url = location;
            }

            var mainData = helper.WebGet(url, cookieCollection);

            var uid = SBHelper.GetValue(mainData, "uid");

            var ts = SBHelper.GetValue(mainData, "ts");

            cookieCollection.Add(new Cookie(SBHelper.UID, uid, "/", ".memory.com"));

            cookieCollection.Add(new Cookie(SBHelper.TS, ts, "/", ".memory.com"));

            cookieCollection.Add(new Cookie(SBHelper.AltHttp, domain, "/", ".memory.com"));

            return cookieCollection;
        }

        readonly Dictionary<string, string> _accounts = new Dictionary<string, string>()
        {
            { "antmatveev81", "dbQXhM2bB"},
            { "graudinagnt0", "sPDg52wQ"},
            { "renshar82", "2T1E5f0t"},
            { "romvitov82", "u56ENHy3"},
            { "inatmn93", "sPDg54wQ"},
            { "lexvasilev79", "72YxfB8f"},
            { "sergejromas97", "N903uKgk"},
            { "egoralikin998", "d4NAZ2D5"},
            { "dmitijkilin99", "CXaasW5h"},
            { "alexeyalex585", "73bR8u7q"},
            { "chelakdilsh", "fkjHfJ8"},
            { "vzakiev24", "4zSsrzsR"},
            { "nabokova85", "3zSsrzsR"},
            { "alivnov86", "8x65U442"},
        };

    }


}
