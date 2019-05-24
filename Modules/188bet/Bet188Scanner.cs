using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bars.EAS.Utils.Extension;
using Bet188.Models;
using BM;
using BM.Core;
using BM.DTO;
using BM.Web;
using Newtonsoft.Json;
using Scanner;

namespace Bet188
{
    public class Bet188Scanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "Bet188";

        public override string Host => "http://landing-sb.prdasbb18a1.com/";

        private string _allInPlay = "/en-gb/sports/all/in-play";
        public static string servicePoint = "en-gb/Service/CentralService?GetData";

        public static readonly List<string> ForbiddenTournaments = new List<string> { "statistics", "cross", "goal", "shot", "offside", "corner", "foul" };

        protected override void UpdateLiveLines()
        {
            var lines = new List<LineDTO>();

            try
            {
                var randomProxy = ProxyList.PickRandom();

                string response;

                var st = new Stopwatch();
                st.Start();

                using (var wc = new PostWebClient(randomProxy))
                {
                    var q = new NameValueCollection
                    {
                        {"IsFirstLoad", "true"},
                        {"VersionL", "-1"},
                        {"VersionU", "0"},
                        {"VersionS", "-1"},
                        {"VersionF", "-1"},
                        {"VersionH", "0"},
                        {"VersionT", "-1"},
                        {"IsEventMenu","false"},
                        {"SportID","1"},
                        {"CompetitionID","-1"},
                        {"reqUrl",_allInPlay},
                        {"oIsInplayAll","false"},
                        {"oIsFirstLoad","true"},
                        {"oSortBy","1"},
                        {"oOddsType","0"},
                        {"oPageNo","0"},
                        {"LiveCenterEventId","0"},
                        {"LiveCenterSportId","0"},

                    };

                    response = wc.UploadString($"{Host}{servicePoint}", ClientExtension.ConstructQueryString(q));
                }

                var result = JsonConvert.DeserializeObject<CentralServiceResult>(response);

                Log.Info($"{Name} Firstquery {st.Elapsed.ToString("g")}");

                st.Restart();

                var events = new List<EventDto>();

                foreach (var ismd in result.lpd.ips.ismd)
                {
                    var d = new EventDto
                    {
                        Sport = Helper.ConvertSport(ismd.sn),
                        SportId = ismd.sid
                    };

                    foreach (var puc in ismd.puc)
                    {
                        var d1 = d.Clone();
                        d1.League = puc.cn;

                        if (ForbiddenTournaments.Any(t => d1.League.ContainsIgnoreCase(t))) continue;

                        foreach (var ces in puc.ces)
                        {
                            var d2 = d1.Clone();
                            d2.Event = ces;

                            events.Add(d2);
                        }
                    }
                }

                var tasks = new Task[events.Count];

                for (var index = 0; index < events.Count; index++)
                {
                    var @event = events[index];
                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var lns = ParseEvent(@event);

                            lock (Lock) lines.AddRange(lns);
                        }
                        catch (Exception e)
                        {
                            Log.Info($"ERROR {Name} Parse event exception {e.Message} {e.StackTrace}");
                        }
                    });

                    tasks[index] = task;
                }

                Task.WaitAll(tasks, 10000);

                Log.Info($"{Name} Other {st.Elapsed:g}");

                st.Stop();

                ActualLines = lines.ToArray();

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));
            }
            catch (Exception e)
            {
                Log.Info($"ERROR {Name} {e.Message} {e.StackTrace}");
            }
        }

        private List<LineDTO> ParseEvent(EventDto @event)
        {
            var random = ProxyList.PickRandom();

            try
            {
                var converter = new Bet188LineConverter();

                var lineTemplate = converter.CreateLine(@event, Host, Name);

                if (lineTemplate == null) return new List<LineDTO>();

                if (!lineTemplate.SportKind.EqualsIgnoreCase("Football") && !lineTemplate.SportKind.EqualsIgnoreCase("Hockey")) return new List<LineDTO>();

                var eventFull = ConverterHelper.GetFullLine(@event, lineTemplate.Url, random, Host);

                if (eventFull == null) return new List<LineDTO>();

                return converter.GetLinesFromEvent(lineTemplate, eventFull);

            }
            catch (WebException e)
            {
                Log.Info($"{Name} WebException " + JsonConvert.SerializeObject(e));
            }
            catch (Exception e)
            {
                Log.Info($"{Name} Parse event exception " + JsonConvert.SerializeObject(e));
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
                    Console.WriteLine($"{Name} check address {host.Address}");
                    using (var wc = new PostWebClient(host))
                    {
                        var q = new NameValueCollection
                        {
                            {"IsFirstLoad", "true"},
                            {"VersionL", "-1"},
                            {"VersionU", "0"},
                            {"VersionS", "-1"},
                            {"VersionF", "-1"},
                            {"VersionH", "0"},
                            {"VersionT", "-1"},
                            {"IsEventMenu","false"},
                            {"SportID","1"},
                            {"CompetitionID","-1"},
                            {"reqUrl",_allInPlay},
                            {"oIsInplayAll","false"},
                            {"oIsFirstLoad","true"},
                            {"oSortBy","1"},
                            {"oOddsType","0"},
                            {"oPageNo","0"},
                            {"LiveCenterEventId","0"},
                            {"LiveCenterSportId","0"},

                        };

                        wc.UploadString($"{Host}{servicePoint}", ClientExtension.ConstructQueryString(q));
                    }
                }
                catch (Exception e)
                {
                    hostsToDelete.Add(host);
                    Log.Info($"{Name} delete address {host.Address} {host.Credentials.GetCredential(host.Address, "").UserName}  listToDelete {hostsToDelete.Count}");
                }
            });

            foreach (var host in hostsToDelete) ProxyList.Remove(host);
        }
    }

    public class EventDto
    {
        public string Sport { get; set; }

        public string League { get; set; }

        public ces Event { get; set; }
        public int SportId { get; set; }
    }
}
