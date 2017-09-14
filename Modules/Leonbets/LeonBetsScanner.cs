using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BM.Core;
using BM.DTO;
using Leonbets.JsonClasses;
using Newtonsoft.Json;
using Scanner;

namespace Leonbets
{
    public class LeonBetsScanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "Leonbets";

        public override string Host => "http://leonbets.net/";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();

        protected override LineDTO[] GetLiveLines()
        {
            try
            {
                var st = new Stopwatch();
                st.Start();

                var hostList = CookieDictionary.Select(c => c.Key).ToList();

                var lines = new List<LineDTO>();

                EventsList data;

                var randHost = hostList.PickRandom();

                using (var webClient = new Extensions.WebClientEx(randHost, CookieDictionary[randHost].GetData()))
                {
                    var d = webClient.DownloadString(Host + "rest/sportsbook/event/listWithMarkets?onlyFavorites=false");
                    data = JsonConvert.DeserializeObject<EventsList>(d);
                }

                var tasks = data.events.AsParallel().WithDegreeOfParallelism(4).Select(@event =>
                    Task.Factory.StartNew(
                        state =>
                        {
                            var proxy = hostList.PickRandom();
                            using (var webClient = new Extensions.WebClientEx(proxy, CookieDictionary[proxy].GetData()))
                            {
                                try
                                {
                                    var json = webClient.DownloadString(string.Format("{1}rest/sportsbook/markets/{0}", @event.Id, Host));
                                    var r = LeonBetsLineConverter.Convert(json, Name, @event);

                                    lock (Lock)
                                    {
                                        lines.AddRange(r);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.Info("LeonBets error" + e.Message + e.InnerException + proxy);
                                }
                            }
                        }, @event)).ToArray();

                try
                {
                    Task.WaitAll(tasks.ToArray(), 10000);
                }
                catch
                {
                    Log.Info("LeonBets Task wait all exception, line count " + lines.Count);
                }

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                Log.Info($"Leonbets Lines {lines.Count} Time { new DateTime(LastUpdatedDiff.Ticks):mm:ss} Proxy {ProxyList.Count}");
                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                return lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info($"ERROR {e.Message} {e.InnerException}");
                Console.WriteLine($"ERROR {e.Message} {e.InnerException}");
            }

            return new LineDTO[] { };
        }
        protected override void CheckDict()
        {
            var listToDelete = new List<WebProxy>();

            Parallel.ForEach(ProxyList, host =>
            {
                CookieDictionary.Add(host, new CachedArray<CookieContainer>(1000 * 3600 * 12, () =>
                    {
                        try
                        {
                            CookieContainer cc;
                            using (var webClient = new Extensions.WebClientEx(host, new CookieContainer()))
                            {
                                var res = webClient.DownloadString($"{Host}rest/sportsbook/event/listWithMarkets?onlyFavorites=false");

                                var i = res.IndexOf("setCookie('", StringComparison.Ordinal);
                                var s = res.Substring(i, 75);

                                var cookieName = s.Split('\'')[1];
                                var cookieValue = s.Split('\'')[3];

                                cc = webClient.CookieContainer;
                                cc.Add(new Cookie(cookieName, cookieValue, "/", Domain));
                            }

                            Console.WriteLine($"check address {host.Address}");

                            using (var webClient = new Extensions.WebClientEx(host, cc))
                            {
                                webClient.DownloadString(Host + "/rest/sportsbook/event/listWithMarkets?onlyFavorites=false");
                            }

                            return cc;
                        }
                        catch (Exception)
                        {
                            listToDelete.Add(host);
                        }

                        return null;
                    }));
            });

            //проверяем работу хоста
            Parallel.ForEach(ProxyList, host => CookieDictionary[host].GetData());

            foreach (var host in listToDelete)
            {
                CookieDictionary.Remove(host);
                ProxyList.Remove(host);
            }

        }
    }
}
