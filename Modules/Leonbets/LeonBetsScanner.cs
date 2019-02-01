using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Web;
using Leonbets.JsonClasses;
using Newtonsoft.Json;
using Scanner;

namespace Leonbets
{
    public class LeonBetsScanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "Leonbets";

        public override string Host => "https://www.leonbets.net/";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();

        public static Regex eventsListRegex = new Regex(@"initialEvents: (?<type>{.+})");

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
                    var d = webClient.DownloadString(Host + "bet-on-live-matches");

                    var f = eventsListRegex.Match(d).Groups["type"].Value;

                    data = JsonConvert.DeserializeObject<EventsList>(f);
                }

                var actualEvents = data.events.Where(e => e.open).ToList();

                var tasks = new List<Task>();

                tasks.AddRange(actualEvents
                    .AsParallel()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(@event =>
                     Task.Factory.StartNew(
                        state =>
                        {
                            var retry = 0;

                            while (retry < 3)
                            {
                                try
                                {
                                    var proxy = hostList.PickRandom();

                                    using (var webClient = new Extensions.WebClientEx(proxy, CookieDictionary[proxy].GetData()))
                                    {
                                        var json = webClient.DownloadString(string.Format("{1}rest/betline/event/inplay?ctag=en-US&eventId={0}", @event.Id, Host));

                                        @event = JsonConvert.DeserializeObject<Event>(json);

                                        var converter = new LeonBetsLineConverter();

                                        var r = converter.Convert(@event, Name);

                                        lock (Lock) lines.AddRange(r);

                                        return;
                                    }
                                }
                                catch (WebException)
                                {
                                    retry++;
                                }
                                catch (Exception e)
                                {
                                    Log.Info("LeonBets error " + JsonConvert.SerializeObject(e));
                                    retry = 3;
                                }
                            }
                        }, @event)));

                try
                {
                    Task.WaitAll(tasks.ToArray(), 10000);
                }
                catch
                {
                    Log.Info("LeonBets Task wait all exception, line count " + lines.Count);
                }

                LastUpdatedDiff = DateTime.Now - LastUpdated;

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

            //Parallel.ForEach(ProxyList, host =>
            //{


            foreach (var host in ProxyList)
            {
                CookieDictionary.Add(host, new CachedArray<CookieContainer>(1000 * 3600 * 6, () =>
                    {
                        try
                        {
                            CookieContainer cc;
                            using (var webClient = new GetWebClient(host))
                            {
                                var res = webClient.DownloadString($"{Host}");

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
                                var f = webClient.DownloadString(Host);

                                if (!f.ContainsIgnoreCase("leonbets")) throw new Exception();
                            }

                            return cc;
                        }
                        catch (Exception e)
                        {
                            listToDelete.Add(host);
                        }

                        return null;
                    }));
            }

            //});

            //проверяем работу хоста
            Parallel.ForEach(ProxyList, host => CookieDictionary[host].GetData());

            //foreach (var webProxy in ProxyList)
            //{
            //    CookieDictionary[webProxy].GetData();
            //}

            foreach (var host in listToDelete)
            {
                CookieDictionary.Remove(host);
                ProxyList.Remove(host);
            }

        }
    }
}
