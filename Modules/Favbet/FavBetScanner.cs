using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Entities;
using BM.Web;
using Extreme.Net;
using Favbet.Models.Line;
using mevoronin.RuCaptchaNETClient;
using Newtonsoft.Json;
using Scanner;
using Scanner.Helper;

namespace Favbet
{
    public class FavBetScanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "Favbet";

        public override string Host => "https://www.favbet.com/";
        //public override string Host => "https://www.favbet.ro/";
        public string DomainForCookie => ".favbet.com";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();

        public static readonly List<string> ForbiddenTournaments = new List<string> { "statistics", "cross", "goal", "shot", "offside", "corner", "foul" };


        protected override LineDTO[] GetLiveLines()
        {
            var lines = new List<LineDTO>();

            try
            {
                var randomProxy = ProxyList.PickRandom();

                string response;

                var cookies = CookieDictionary[randomProxy].GetData().GetAllCookies();

                using (var wc = new PostWebClient(randomProxy, cookies))
                {
                    response = wc.UploadString($"{Host}frontend_api/events_short/", "{\"service_id\":1,\"lang\":\"en\"}");
                }

                var sportids = JsonConvert.DeserializeObject<EventsShort>(response).Events.Select(e => e.sport_id).Distinct().ToList();

                var events = new List<Event>();

                Parallel.ForEach(sportids, sportId =>
                {
                    try
                    {
                        var random = ProxyList.PickRandom();
                        var cook = CookieDictionary[random].GetData().GetAllCookies();

                        using (var wc = new PostWebClient(random, cook))
                        {
                            response = wc.UploadString($"{Host}frontend_api/events/", $"{{\"service_id\":1,\"lang\":\"en\",\"sport_id\":{sportId}}}");
                            var e = JsonConvert.DeserializeObject<EventsShort>(response).Events;

                            lock (Lock) events.AddRange(e);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Info("Get event exception");
                    }
                });


                var tasks = new List<Task>();

                tasks.AddRange(events
                    .AsParallel()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(@event =>
                    Task.Factory.StartNew(state =>
                    {
                        //убираем запрещенные чемпионаты
                        if (@event.tournament_name.ContainsIgnoreCase(ForbiddenTournaments.ToArray())) return;
                        if (@event.event_name.ContainsIgnoreCase(ForbiddenTournaments.ToArray())) return;

                        var lns = ParseEvent(@event);

                        lock (Lock) lines.AddRange(lns);

                    }, @event)));

                try
                {
                    Task.WaitAll(tasks.ToArray(), 10000);
                }
                catch (Exception e)
                {
                    Log.Info("FavBet Task wait all exception, line count " + lines.Count);
                    Console.WriteLine("FavBet Task wait all exception, line count " + lines.Count);
                }

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count(c => c != null), new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                return lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info($"ERROR FB {e.Message} {e.StackTrace}");
            }

            return new LineDTO[] { };
        }

        private List<LineDTO> ParseEvent(Event @event)
        {
            var random = ProxyList.PickRandom();

            var c = CookieDictionary[random].GetData();

            try
            {
                var converter = new FavBetConverter();

                var lineTemplate = converter.CreateLine(@event, Host, Name);

                if (lineTemplate == null) return new List<LineDTO>();

                var markets = ConverterHelper.GetMarketsByEvent(@event.event_id, random, c, Host);

                if (markets == null) return new List<LineDTO>();

                return converter.GetLinesFromEvent(lineTemplate, markets);

            }
            catch (WebException e)
            {
                Log.Info("Favbet WebException " + JsonConvert.SerializeObject(e));
                ParseEvent(@event);
            }
            catch (Exception e)
            {
                Log.Info("FB Parse event exception " + JsonConvert.SerializeObject(e) + JsonConvert.SerializeObject(c.GetAllCookies()));
            }

            return new List<LineDTO>();
        }


        protected override void CheckDict()
        {
            var listToDelete = new List<WebProxy>();

            foreach (var host in ProxyList)
            {
                CookieDictionary.Add(host, new CachedArray<CookieContainer>(1000 * 3600 * 3, () =>
                {
                    try
                    {
                        var cc = new CookieContainer();

                        ConsoleExt.ConsoleWriteError($"Favbet check address {host.Address}");

                        cc.Add(PassCloudFlare(host));

                        using (var wc = new Extensions.WebClientEx(host, cc))
                        {
                            wc.Headers["User-Agent"] = GetWebClient.DefaultUserAgent;

                            wc.DownloadString(Host + "en/live/");

                            var d = wc.ResponseHeaders["Set-Cookie"];

                            foreach (var match in d.Split(',').Select(singleCookie => Regex.Match(singleCookie, "(.+?)=(.+?);")).Where(match => match.Captures.Count != 0))
                            {
                                var name = match.Groups[1].ToString();
                                var value = match.Groups[2].ToString();
                                if (name == "PHPSESSID") cc.Add(new Cookie(name, value) { Domain = ProxyHelper.GetDomain(Host) });
                            }

                            cc.Add(wc.CookieContainer.GetAllCookies());
                        }

                        return cc;
                    }
                    catch (Exception e)
                    {
                        listToDelete.Add(host);
                        ConsoleExt.ConsoleWriteError($"Favbet delete address {host.Address} listToDelete {listToDelete.Count}");
                    }

                    return null;
                }));
            }

            var tasks = ProxyList.AsParallel().Select(host => Task.Factory.StartNew(state => CookieDictionary[host].GetData(), host)).ToArray();

            //foreach (var host in ProxyList)
            //{
            //    CookieDictionary[host].GetData();
            //}

            Task.WaitAll(tasks.ToArray());

            foreach (var host in listToDelete)
            {
                CookieDictionary.Remove(host);
                ProxyList.Remove(host);
            }
        }

        private CookieCollection PassCloudFlare(WebProxy proxy)
        {
            var cookieCollection = new CookieCollection();

            #region Cloudflare wait 5 sec

            var cookies = CloudFlareNet.CloudFlareNet.GetCloudflareCookies(Host + "en/live/", GetWebClient.DefaultUserAgent, new HttpProxyClient(proxy.Address.Host, proxy.Address.Port, proxy.Credentials.GetCredential(proxy.Address, "").UserName, proxy.Credentials.GetCredential(proxy.Address, "").Password));

            if (cookies != null && cookies.Any())
            {
                foreach (var cookie in cookies)
                {
                    cookieCollection.Add(new Cookie(cookie.Key, cookie.Value, "/", DomainForCookie));
                }
            }
            else
                //ReCaptcha
                cookieCollection = CloudflareRecaptcha(proxy);

            return cookieCollection;

            #endregion
        }

        private CookieCollection CloudflareRecaptcha(WebProxy proxy)
        {
            var cookieCollection = new CookieCollection();

            string responseText;

            using (var webClient = new GetWebClient(proxy, cookieCollection))
            {
                try
                {
                    responseText = webClient.DownloadString(Host + "en/live/");
                    cookieCollection.Add(webClient.CookieCollection);
                }
                catch (WebException ex)
                {
                    var response = (HttpWebResponse)ex.Response;

                    var encoding = Encoding.ASCII;
                    using (var reader = new StreamReader(response.GetResponseStream(), encoding)) responseText = reader.ReadToEnd();

                    cookieCollection.Add(response.Cookies);
                }
            }

            if (!string.IsNullOrEmpty(responseText) && responseText.ContainsIgnoreCase("sport")) return cookieCollection;

            var ray = Regex.Match(responseText, "data-ray=\"(.+?)\"").Groups[1].Value;
            var sitekey = Regex.Match(responseText, "data-sitekey=\"(.+?)\"").Groups[1].Value;
            var stoken = Regex.Match(responseText, "data-stoken=\"(.+?)\"").Groups[1].Value;

            Log.Info($"FavBet RECAPTCHA Start {sitekey} {stoken}");

            var captchaResponse = RuCaptchaHelper.GetCaptchaResult(sitekey, stoken, Host, proxy.Credentials.GetCredential(proxy.Address, "").UserName, proxy.Address.Host, proxy.Credentials.GetCredential(proxy.Address, "").Password, proxy.Address.Port);

            Log.Info($"FavBet RECAPTCHA Result {captchaResponse}");

            if (captchaResponse.Contains("ERROR")) throw new Exception("Error on ReCaptcha resolve");

            using (var webClient = new GetWebClient(proxy, cookieCollection))
            {
                webClient.DownloadData($"{Host}cdn-cgi/l/chk_captcha?id={ray}&g-recaptcha-response={captchaResponse}");
                cookieCollection.Add(webClient.CookieCollection);
            }

            return cookieCollection;
        }
    }
}
