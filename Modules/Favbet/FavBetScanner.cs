using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Web;
using Favbet.Models.Line;
using Newtonsoft.Json;
using Scanner;
using Scanner.Helper;

namespace Favbet
{
    public class FavBetScanner : ScannerBase
    {
        public override string Name => "Favbet";

        //public override string Host => "https://www.favbet.com/";
        public override string Host => "https://www.favbet.biz/";
        public string DomainForCookie => ".favbet.ro";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();

        public static readonly List<string> ForbiddenTournaments = new List<string> { "statistics", "cross", "goal", "shot", "offside", "corner", "foul" };

        object _lock = new object();

        protected override void UpdateLiveLines()
        {
            var lines = new List<LineDTO>();

            try
            {
                var randomProxy = ProxyList[I];

                string response;

                var cookies = CookieDictionary[randomProxy].GetData().GetAllCookies();

                try
                {
                    using (var wc = new PostWebClient(randomProxy, cookies))
                    {
                        response = wc.UploadString($"{Host}frontend_api/events_short/", "{\"service_id\":1,\"lang\":\"en\"}");
                    }
                }
                catch (WebException e)
                {
                    //ConsoleExt.ConsoleWriteError($"{Name} Get event WebException {e.Message}");

                    return;
                }

                var sportids = JsonConvert.DeserializeObject<EventsShort>(response).Events.Select(e => e.sport_id).Distinct().ToList();

                var events = new List<Event>();

                Parallel.ForEach(sportids, sportId =>
                {
                    try
                    {
                        var random = ProxyList[I];
                        var cook = CookieDictionary[random].GetData().GetAllCookies();

                        using (var wc = new PostWebClient(random, cook))
                        {
                            response = wc.UploadString($"{Host}frontend_api/events/", $"{{\"service_id\":1,\"lang\":\"en\",\"sport_id\":{sportId}}}");
                            var e = JsonConvert.DeserializeObject<EventsShort>(response).Events;

                            lock (_lock) events.AddRange(e);
                        }
                    }
                    catch (WebException e)
                    {
                        //ConsoleExt.ConsoleWriteError($"{Name} Get event WebException {e.Message}");
                    }
                    catch (Exception e)
                    {
                        ConsoleExt.ConsoleWriteError($"{Name} Get event exception {e.Message}");
                    }
                });


                var tasks = new Task[events.Count];

                for (var i = 0; i < events.Count; i++)
                {
                    var @event = events[i];

                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            //убираем запрещенные чемпионаты
                            if (@event.tournament_name.ContainsIgnoreCase(ForbiddenTournaments.ToArray())) return;
                            if (@event.event_name.ContainsIgnoreCase(ForbiddenTournaments.ToArray())) return;

                            var lns = ParseEvent(@event);

                            lock (_lock) lines.AddRange(lns);
                        }
                        catch (WebException e)
                        {
                            //ConsoleExt.ConsoleWriteError($"{Name} Parse event exception {e.Message}");
                        }
                        catch (Exception e)
                        {
                            ConsoleExt.ConsoleWriteError($"{Name} Parse event exception {e.Message} {e.StackTrace}");
                        }
                    });

                    tasks[i] = task;
                }

                Task.WaitAll(tasks,10000);

                ActualLines = lines.ToArray();

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));
            }
            catch (Exception e)
            {
                ConsoleExt.ConsoleWriteError($"ERROR FB {e.Message} {e.StackTrace}");
            }
        }

        private List<LineDTO> ParseEvent(Event @event)
        {
            var random = ProxyList[I];

            var c = CookieDictionary[random].GetData();

            try
            {
                var converter = new FavBetConverter();

                var lineTemplate = converter.CreateLine(@event, Host, Name);

                if (lineTemplate == null) return new List<LineDTO>();

                var markets = ConverterHelper.GetMarketsByEvent(@event.event_id, random, c, Host);

                return markets == null ? new List<LineDTO>() : converter.GetLinesFromEvent(lineTemplate, markets);
            }
            catch (WebException e)
            {
                //ConsoleExt.ConsoleWriteError($"{Name} WebException { e.Message}");
                //ParseEvent(@event);
            }
            catch (Exception e)
            {
                ConsoleExt.ConsoleWriteError($"{Name} Parse event exception " + JsonConvert.SerializeObject(e));
            }

            return new List<LineDTO>();
        }


        protected override void CheckDict()
        {
            var listToDelete = new List<WebProxy>();

            foreach (var host in ProxyList)
            {
                CookieDictionary.Add(host, new CachedArray<CookieContainer>(1000 * 60 * 30, () =>
                {
                    try
                    {
                        var cc = new CookieContainer();

                        ConsoleExt.ConsoleWriteError($"Favbet check address {host.Address}");

                        //cc.Add(PassCloudFlare(host));

                        using (var wc = new PostWebClient(host))
                        {

                            wc.Headers["User-Agent"] = GetWebClient.DefaultUserAgent;

                            var query = $"{{\"jsonrpc\":\"2.0\",\"method\":\"frontend/sport/get\",\"params\":{{}},\"id\":0}}";

                            wc.UploadString($"{Host}frontend_api2/", query);

                            //foreach (var market in t.Result)
                            //{
                            //    foreach (var outcome in market.outcomes)
                            //    {
                            //        ProxyHelper.UpdateFavbetEvents(market.market_name + " | " + market.result_type_name + " | " + outcome.outcome_name + " | " +outcome.outcome_param);
                            //    }
                            //}

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

            Task.WaitAll(tasks);

            foreach (var host in listToDelete)
            {
                CookieDictionary.Remove(host);
                ProxyList.Remove(host);
            }
        }
    }
}
