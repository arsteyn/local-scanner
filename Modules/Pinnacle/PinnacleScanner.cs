using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Web;
using Newtonsoft.Json;
using Pinnacle.JsonClasses;
using Scanner;

namespace Pinnacle
{
    public class PinnacleScanner : ScannerBase
    {
        public override string Name => "Pinnacle";

        public override string Host => "https://members.pinnacle.com/";

        public Dictionary<WebProxy, CachedArray<CookieCollection>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieCollection>>();

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

        readonly object _lock = new object();

        protected override void UpdateLiveLines()
        {
            var st = new Stopwatch();

            st.Start();

            try
            {
                var randomProxy = ProxyList.PickRandom();

                var cookie = CookieDictionary[randomProxy].GetData();

                var lines = new List<LineDTO>();

                foreach (var sport in _sportsList)
                {
                    var url = BuildUrl(sport);

                    //Get periods and levels
                    RequestResult result;

                    using (var client = new GetWebClient(randomProxy, cookie)){
                        result = client.DownloadResult<RequestResult>(url);
                    }

                    var buySellLevels = result.Sport.Markets.First(m => m.MarketName.ContainsIgnoreCase("live")).GamesContainers.ToDictionary(gameContainer => gameContainer.Value.LeagueId.ToString(), gameContainer => 3);

                    Parallel.ForEach(result.Sport.Periods, period =>
                    {

                        try
                        {
                            string response;

                            var converter = new PinnacleLineConverter();

                            var random = ProxyList.PickRandom();

                            var cookies = CookieDictionary[randomProxy].GetData();

                            using (var client = new GetWebClient(random, cookies)) response = client.DownloadString(BuildUrl(sport, period.Id, buySellLevels));

                            var l = converter.Convert(response, Name);

                            lock (_lock) lines.AddRange(l);
                        }
                        catch (Exception e)
                        {
                            Log.Info($"ERROR Pinnacle inner {e.Message} {e.StackTrace} {e.InnerException}");
                        }
                    });
                }

                ActualLines = lines.ToArray();

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));
            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.ProtocolError)
                {
                    Log.Info($"ERROR Pinnacle {ex.Message} {ex.StackTrace}");
                }
                else if (ex.Response is HttpWebResponse response)
                {
                    if ((int)response.StatusCode != 419) Log.Info($"ERROR Pinnacle {ex.Message} {ex.StackTrace}");
                }
                else
                    Log.Info($"ERROR Pinnacle {ex.Message} {ex.StackTrace}");
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Pinnacle {e.Message} {e.StackTrace} {e.InnerException}");
            }
        }

        private readonly List<string> _sportsList = new List<string>
        {
            "Soccer",
            //"Tennis"
            //"Hockey"
        };

        private string BuildUrl(string sport, int period = 0, Dictionary<string, int> buySellLevels = null)
        {
            var url = $"{Host}Sportsbook/Asia/en-GB/GetLines/{sport}/Live/1/{period}/null/Regular/SportsBookAll/Decimal/7/false/";

            if (buySellLevels != null) url += "?buySellLevels=" + HttpUtility.UrlEncode(JsonConvert.SerializeObject(buySellLevels));

            return url;
        }


        protected override void CheckDict()
        {
            foreach (var account in _accounts)
            {
                for (int i = ProxyList.Count - 1; i >= 0; i--)
                {
                    var host = ProxyList[i];

                    if (CookieDictionary.ContainsKey(host)) continue;

                    CookieDictionary.Add(host, new CachedArray<CookieCollection>(1000 * 60 * 15, () =>
                    {
                        try
                        {
                            return Authorize(host, account.Key, account.Value);
                        }
                        catch (Exception)
                        {
                            ConsoleExt.ConsoleWriteError($"Pinnacle delete address {host.Address}");
                        }

                        return null;
                    }));

                    if (CookieDictionary[host].GetData() != null) break;

                    CookieDictionary.Remove(host);

                    ProxyList.RemoveAt(i);
                }
            }

            foreach (var host in ProxyList.OrderBy(p => Guid.NewGuid()))
            {
                if (!CookieDictionary.ContainsKey(host)) ProxyList.Remove(host);
            }
        }

        private CookieCollection Authorize(WebProxy host, string login, string password)
        {
            var cookieCollection = new CookieCollection();

            var query = new NameValueCollection
            {
                { "fakeusernameremembered", string.Empty},
                { "fakepasswordremembered", string.Empty},
                { "CustomerId",login},
                { "Password", password},
                { "AppId", "Classic" },
            };

            cookieCollection.Add(new Cookie("ADRUM", $"s={(long)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds}&r=https%3A%2F%2F{new Uri(Host).Host}%2Fen%2F%3F0", "/", new Uri(Host).Host));


            using (var webClient = new PostWebClient(host, cookieCollection))
            {

                webClient.ContentType = "application/x-www-form-urlencoded";

                webClient.Post($"{Host}login/authenticate/Classic/en-GB", query);

                cookieCollection.Add(webClient.CookieCollection);
            }

            return cookieCollection;
        }

        readonly Dictionary<string, string> _accounts = new Dictionary<string, string>()
        {
            //{ "VB982035", "qAYQ7Y_7c"}, заблочен
            { "AS983383", "xuW987Q_K1"}, //ok
            { "MK1002615", "89XS_1ZAh"}, //ok
            { "GM1001709", "02Pt7_yEk"}, //ok
            { "SK1004637", "aru4598_HFYfg"}, //ok
            { "RC1001720", "2T%1E5f0t"}, //ok
            { "KK991446", "asdDd84%dj4k"}, //ok
            { "MG992240", "Gt_4ZVW9Q"}, //ok
            { "AV1014482", "72Yx_fB8f"},//ok
            { "RD1017085", "4wUp7C%S2"},//ok
            { "MK1020538", "2p9%Vvk3v"},//ok
            { "RV1030413", "u56EN_Hy3"},//ok

        };

    }


}
