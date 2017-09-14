using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BM.DTO;
using BM.Web;
using Bwin.JsonClasses;
using Newtonsoft.Json;
using Scanner;

namespace Bwin
{
    public class BwinScanner : ScannerBase
    {
        private static readonly object Lock = new object();

        public override string Name => "Bwin";

        public override string Host => "https://livebetting.bwin.com/";


        protected override LineDTO[] GetLiveLines()
        {
            try
            {
                var st = new Stopwatch();

                st.Start();

                var lines = new List<LineDTO>();

                var randHost = ProxyList.PickRandom();

                List<Event> events;

                using (var webClient = new PostWebClient(randHost))
                {
                    events = JsonConvert.DeserializeObject<EventsResponce>(webClient.Post($"{Host}en/live/secure/api/betoffer/liveEvents")).events;
                }


                var tasks = events.AsParallel().WithDegreeOfParallelism(4).Select(@event =>
                    Task.Factory.StartNew(
                        state =>
                        {
                            var proxy = ProxyList.PickRandom();

                            using (var wc = new PostWebClient(proxy))
                            {
                                try
                                {
                                    var query = new NameValueCollection
                                    {
                                        {"id", @event.Id},
                                        {"full", "True"}
                                    };

                                    var responce = wc.Post($"{Host}en/live/secure/api/betoffer/event", query);

                                    lock (Lock)
                                    {
                                        lines.AddRange(BwinLineConverter.Convert(Name, responce));
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.Info("Bwin error" + e.Message + e.InnerException + proxy);
                                }
                            }
                        }, @event)).ToArray();
                try
                {
                    Task.WaitAll(tasks.ToArray(), 10000);
                }
                catch
                {
                    Log.Info("Bwin Task wait all exception, line count " + lines.Count);
                    Console.WriteLine("Bwin Task wait all exception, line count " + lines.Count);
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
            var hostsToDelete = new List<WebProxy>();

            Parallel.ForEach(ProxyList, (host, state) =>
            {
                try
                {
                    using (var webClient = new Extensions.WebClientEx(host))
                    {
                        Console.WriteLine($"Bwin Lines Check Proxy {host.Address}");
                        webClient.DownloadString(Host);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"Bwin Lines DELETE Proxy {host.Address}");
                    hostsToDelete.Add(host);
                }
            });

            foreach (var host in hostsToDelete) ProxyList.Remove(host);
        }
    }
}
