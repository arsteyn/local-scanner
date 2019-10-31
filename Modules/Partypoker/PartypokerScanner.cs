using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BM.DTO;
using BM.Web;
using Newtonsoft.Json;
using Partypoker.JsonClasses;
using Scanner;

namespace Partypoker
{
    public class PartypokerScanner : ScannerBase
    {
        private static readonly object Lock = new object();

        public override string Name => "Partypoker";

        public override string Host => "https://bcdapi.partypoker.com/";


        protected override void UpdateLiveLines()
        {
            try
            {
                var st = new Stopwatch();

                st.Start();

                var lines = new List<LineDTO>();

                var randHost = ProxyList.PickRandom();

                var events = new List<long>();

                string pushAccessId;

                using (var webClient = new GetWebClient(randHost))
                {
                    var s = webClient.DownloadString($"https://sports.partypoker.com/en/client-bootstrap-scripts.js");
                    pushAccessId = s.RegexStringValue("\"pushAccessId\":\"(?<value>.*?)\"");
                }

                //Football
                using (var webClient = new GetWebClient(randHost))
                {
                    var f = webClient.DownloadString(
                        "https://cds-api.partypoker.com/bettingoffer/fixtures?" +
                        $"x-bwin-accessid={pushAccessId}&" +
                        "lang=en&" +
                        "country=RU&" +
                        "userCountry=RU&" +
                        "state=Live&" +
                        "take=9999&" +
                        "offerMapping=Filtered&" +
                        "offerCategories=Gridable&" +
                        "sortBy=StartDate&" +
                        "sportIds=4"
                        );

                    var x = JsonConvert.DeserializeObject<FixturesResponse>(f);

                    events.AddRange(x.fixtures.Select(g => g.id));
                }

                //Ice hockey
                using (var webClient = new GetWebClient(randHost))
                {
                    var f = webClient.DownloadString(
                        "https://cds-api.partypoker.com/bettingoffer/fixtures?" +
                        $"x-bwin-accessid={pushAccessId}&" +
                        "lang=en&" +
                        "country=RU&" +
                        "userCountry=RU&" +
                        "state=Live&" +
                        "take=9999&" +
                        "offerMapping=Filtered&" +
                        "offerCategories=Gridable&" +
                        "sortBy=StartDate&" +
                        "sportIds=12"
                    );

                    var x = JsonConvert.DeserializeObject<FixturesResponse>(f);

                    events.AddRange(x.fixtures.Select(g => g.id));
                }

                var tasks = new Task[events.Count];

                for (var i = 0; i < events.Count; i++)
                {
                    var @event = events[i];
                    var task = Task.Factory.StartNew(() =>
                    {
                        var proxy = ProxyList.PickRandom();

                        using (var wc = new GetWebClient(proxy))
                        {
                            try
                            {

                                var responce = wc.DownloadString($"https://cds-api.partypoker.com/bettingoffer/fixture-view?" +
                                                                 $"x-bwin-accessid={pushAccessId}&" +
                                                                 $"lang=en&country=UA&" +
                                                                 $"userCountry=UA&" +
                                                                 $"offerMapping=All&" +
                                                                 $"scoreboardMode=Full&" +
                                                                 $"fixtureIds={@event}&" +
                                                                 $"state=Live"
                                );

                                var converter = new PartypokerLineConverter();
                                var res = converter.Convert(Name, responce);

                                lock (Lock) lines.AddRange(res);
                            }
                            catch (WebException e)
                            {
                                //ConsoleExt.ConsoleWriteError($"{Name} WebException {e.Message}");
                            }
                            catch (Exception e)
                            {
                                ConsoleExt.ConsoleWriteError($"{Name} error {e.Message}");
                            }
                        }
                    });

                    tasks[i] = task;
                }

                Task.WaitAll(tasks, 10000);

                ActualLines = lines.ToArray();

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Partypoker {e.Message} {e.StackTrace}");
            }
        }

        protected override void CheckDict()
        {
            var hostsToDelete = new List<WebProxy>();

            Parallel.ForEach(ProxyList, (host, state) =>
            {
                try
                {
                    using (var webClient = new Extensions.WebClientEx(host))
                    {
                        Console.WriteLine($"{Name} Lines Check Proxy {host.Address}");
                        webClient.DownloadString("https://sports.partypoker.com/en/sports");
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"{Name} Lines DELETE Proxy {host.Address}");
                    hostsToDelete.Add(host);
                }
            });

            foreach (var host in hostsToDelete) ProxyList.Remove(host);
        }
    }
}
