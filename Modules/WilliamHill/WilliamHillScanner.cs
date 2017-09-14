using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using BM.DTO;
using Scanner;
using Extensions = Scanner.Extensions;

namespace WilliamHill
{
    public class WilliamHillScanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "WilliamHill";

        public override string Host => "http://sports.williamhill.com/";

        private List<string> UpdateUrls()
        {
            var st = new Stopwatch();
            st.Start();

            string s;

            var proxy = ProxyList.PickRandom();

            using (var webClient = new Extensions.WebClientEx(proxy)) s = webClient.DownloadString($"{Host}bir_xml?action=miniApp");

            var document = XDocument.Parse(s);

            var ids = from m in document.Elements("miniApp").Elements("Events").Elements("Event")
                      select m.Attribute("ob_id").Value;

            var urls = ids
                .Select(x => string.Format("{1}/bir_xml?action=event&version=1&ev_id={0}", x, Host))
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

                var converter = new WilliamHillLineConverter();

                var lines = new List<LineDTO>();
                { };

                var tasks = urls.AsParallel().WithDegreeOfParallelism(4).Select(@event =>
                    Task.Factory.StartNew(
                        state =>
                        {
                            var randHost2 = ProxyList.PickRandom();
                            using (var webClient = new Extensions.WebClientEx(randHost2))
                            {
                                try
                                {
                                    var json = webClient.DownloadString(@event);
                                    var r = converter.Convert(json, Name);
                                    lock (Lock)
                                    {
                                        lines.AddRange(r);
                                    }
                                }
                                catch (Exception e)
                                {
                                    //Log.Info("WH Parse event exception " + e.InnerException);
                                    //Console.WriteLine("WH Parse event exception " + e.InnerException);
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
                        XDocument.Parse(webClient.DownloadString(Host + "bir_xml?action=miniApp"));
                    }
                }
                catch (Exception)
                {
                    hostsToDelete.Add(host);
                }
            });

            foreach (var host in hostsToDelete) ProxyList.Remove(host);
        }
    }
}
