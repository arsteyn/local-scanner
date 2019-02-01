using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using BM.DTO;
using Newtonsoft.Json;
using Scanner;
using static System.String;
using Extensions = Scanner.Extensions;

namespace WilliamHill
{
    public class WilliamHillScanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "WilliamHill";

        public override string Host => "https://sports.williamhill.com/";

        private List<string> UpdateUrls()
        {
            var st = new Stopwatch();
            st.Start();

            var s = Empty;

            var proxy = ProxyList.PickRandom();
            var retry = 0;

            while (retry < 3)
            {
                try
                {
                    using (var webClient = new Extensions.WebClientEx(proxy))
                        s = webClient.DownloadString($"{Host}bir_xml?action=miniApp");

                    retry = 3;
                }
                catch (WebException)
                {
                    retry++;
                }
                catch (Exception e)
                {
                    Log.Info("WilliamHill UpdateUrls error " + JsonConvert.SerializeObject(e));
                    retry = 3;
                }
            }

            var document = XDocument.Parse(s);

            var ids = from m in document.Elements("miniApp").Elements("Events").Elements("Event")
                      select m.Attribute("ob_id").Value;

            var urls = ids
                .Select(x => Format("{1}bir_xml?action=event&version=1&ev_id={0}", x, Host))
                .ToList();

            return urls;
        }

        protected override LineDTO[] GetLiveLines()
        {
            try
            {
                var st = new Stopwatch();
                st.Start();

                var urls = UpdateUrls();

                var lines = new List<LineDTO>();

                var tasks = urls.AsParallel().WithDegreeOfParallelism(4).Select(@event =>
                    Task.Factory.StartNew(
                        state =>
                        {
                            var retry = 0;
                            while (retry < 3)
                            {
                                var proxy = ProxyList.PickRandom();

                                try
                                {
                                    using (var webClient = new Extensions.WebClientEx(proxy))
                                    {
                                        var json = webClient.DownloadString(@event);

                                        var converter = new WilliamHillLineConverter();

                                        var r = converter.Convert(json, Name);

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
                                    Log.Info("WilliamHill parse error " + JsonConvert.SerializeObject(e));
                                    retry = 3;
                                }
                            }
                        }, @event)).ToArray();
                try
                {
                    Task.WaitAll(tasks.ToArray(), 10000);
                }
                catch
                {
                    Console.WriteLine("WilliamHill Task wait all exception, line count " + lines.Count);
                    Log.Info("WilliamHill Task wait all exception, line count " + lines.Count);
                }


                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));


                return lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info("ERROR WH " + e.Message + e.InnerException + e.StackTrace);
                //Console.WriteLine("ERROR WH " + e.Message + e.InnerException + e.StackTrace);
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
                    Console.WriteLine($"WH check address {host.Address}");
                    using (var webClient = new Extensions.WebClientEx(host))
                    {
                        var t = webClient.DownloadString(Host + "bir_xml?action=miniApp");

                        XDocument.Parse(t);
                    }
                }
                catch (Exception e)
                {
                    hostsToDelete.Add(host);
                    Log.Info($"Wh delete address {host.Address} {host.Credentials.GetCredential(host.Address, "").UserName}  listToDelete {hostsToDelete.Count}");
                }
            });

            foreach (var host in hostsToDelete) ProxyList.Remove(host);
        }
    }
}
