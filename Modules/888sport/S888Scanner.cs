using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bars.EAS.Utils.Extension;
using BM.DTO;
using BM.Web;
using Newtonsoft.Json;
using S888.Models.Line;
using Scanner;

namespace S888
{
    public class S888Scanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "S888";

        public override string Host => "https://eu-offering.kambicdn.org/";

        public static readonly List<string> ForbiddenTournaments = new List<string> { "statistics", "cross", "goal", "shot", "offside", "corner", "foul" };

        protected override void UpdateLiveLines()
        {
            var lines = new List<LineDTO>();

            try
            {
                var randomProxy = ProxyList.PickRandom();

                string response;

                using (var wc = new GetWebClient(randomProxy))
                {
                    response = wc.DownloadString($"{Host}offering/v2018/888/listView/all/all/all/all/in-play.json?lang=en_GB&market=En");
                }

                var events = JsonConvert.DeserializeObject<EventResult>(response).Events.Where(e => e.Event.path.All(p => !ForbiddenTournaments.Any(t => p.englishName.ContainsIgnoreCase(t)))).ToList();

                Parallel.ForEach(events, @event =>
                {
                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var lns = ParseEvent(@event);

                            lock (Lock) lines.AddRange(lns);
                        }
                        catch (Exception e)
                        {
                            Log.Info($"ERROR S888 Parse event exception {e.Message} {e.StackTrace}");
                        }
                    });

                    if (!task.Wait(10000)) Log.Info("S888 Task wait exception");
                });

                ActualLines = lines.ToArray();

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));
            }
            catch (Exception e)
            {
                Log.Info($"ERROR S888 {e.Message} {e.StackTrace}");
            }
        }

        private List<LineDTO> ParseEvent(EventSub @event)
        {
            var random = ProxyList.PickRandom();

            try
            {
                var converter = new S888LineConverter();

                var lineTemplate = converter.CreateLine(@event, Host, Name);

                if (lineTemplate == null) return new List<LineDTO>();

                if (!lineTemplate.SportKind.EqualsIgnoreCase("Football") && !lineTemplate.SportKind.EqualsIgnoreCase("Hockey")) return new List<LineDTO>();

                var eventFull = ConverterHelper.GetFullLine(@event.Event.id, random, Host);

                if (eventFull == null) return new List<LineDTO>();

                return converter.GetLinesFromEvent(lineTemplate, eventFull);

            }
            catch (WebException e)
            {
                Log.Info("888sport WebException " + JsonConvert.SerializeObject(e));
                ParseEvent(@event);
            }
            catch (Exception e)
            {
                Log.Info("888sport Parse event exception " + JsonConvert.SerializeObject(e));
            }

            return new List<LineDTO>();
        }


        protected override void CheckDict()
        {
            var hostsToDelete = new List<WebProxy>();

            Parallel.ForEach(ProxyList, (host, state) =>
            {
                try
                {
                    Console.WriteLine($"888sport check address {host.Address}");
                    using (var webClient = new Extensions.WebClientEx(host))
                    {
                        webClient.DownloadString("https://eu-offering.kambicdn.org/offering/v2018/888/listView/all/all/all/all/in-play.json?lang=en_GB&market=En");
                    }
                }
                catch (Exception e)
                {
                    hostsToDelete.Add(host);
                    Log.Info($"888sport delete address {host.Address} {host.Credentials.GetCredential(host.Address, "").UserName}  listToDelete {hostsToDelete.Count}");
                }
            });

            foreach (var host in hostsToDelete) ProxyList.Remove(host);
        }
    }
}
