using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Web;
using Dafabet.Models;
using Newtonsoft.Json;
using Scanner;
using Scanner.Helper;

namespace Dafabet
{
    public class DafabetScanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "Dafabet";

        public override string Host => "https://www.sportdafa.net/";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();

        public static readonly string[] LeagueStopWords = {
            "fantasy",
            "corner",
            "specific",
            "statistics",
            "crossbar",
            "goalpost",
            "fouls",
            "offsides",
            "shot",
            "booking",
            "penalty",
            "special",
            "goal",
            "kick",
            "offside",
            "throw",
            "over",
            "under",
            //penalty
            "(PEN)",
            //extra time
            "(ET)"
        };

        protected override LineDTO[] GetLiveLines()
        {
            var lines = new List<LineDTO>();

            var randomProxy = ProxyList.PickRandom();

            try
            {
                var matchList = new List<KeyValuePair<string, long>>();

                var cookies = CookieDictionary[randomProxy].GetData();

                using (var client = new Extensions.WebClientEx(randomProxy, cookies))
                {
                    client.Headers["Referer"] = $"{Host.Replace("www", "prices")}EuroSite/Euro_index.aspx";

                    var u = $"{Host.Replace("www", "prices")}EuroSite/match_data.ashx?Scope=Sport&SportType=0&FixtureType=l";

                    string response = client.DownloadString(u);

                    var t = JsonConvert.DeserializeObject<MatchDataResult>(response);

                    foreach (var league in t.leagues)
                    {
                        //убираем запрещенные чемпионаты
                        if (league.LeagueName.ContainsIgnoreCase(LeagueStopWords.ToArray())) continue;

                        matchList.AddRange(league.matches.Where(m => m.IsLive).Select(m => new KeyValuePair<string, long>(league.SportName, m.MatchId)).ToList());
                    }
                }

                var tasks = new List<Task>();

                var matches = matchList.ToList();

                tasks.AddRange(matches
                    .AsParallel()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(match =>
                    Task.Factory.StartNew(state =>
                    {
                        try
                        {
                            var leagueUrl = $"{Host.Replace("www", "prices")}EuroSite/match_data.ashx?Game=0&Scope=Match&SportType={match.Key}&FixtureType=l&Id={match.Value}";

                            var random = ProxyList.PickRandom();

                            using (var cl = new Extensions.WebClientEx(random, CookieDictionary[randomProxy].GetData()))
                            {
                                cl.Headers["Referer"] = $"{Host.Replace("www", "prices")}EuroSite/Euro_index.aspx";

                                var response = cl.DownloadString(leagueUrl);

                                var converter = new DafabetConverter();

                                var l = converter.Convert(response, Name);

                                lock (Lock) lines.AddRange(l);
                            }
                        }
                        catch (WebException e)
                        {
                            Log.Info("Dafabet WebException " + e.Message + e.InnerException.Message + e.StackTrace);
                        }
                        catch (Exception e)
                        {
                            Log.Info("Dafabet Parse match exception " + e.Message + e.InnerException.Message + e.StackTrace);
                        }

                    }, match)));

                try
                {
                    Task.WaitAll(tasks.ToArray(), 10000);
                }
                catch (Exception e)
                {
                    Log.Info("Dafabet Task wait all exception, line count " + lines.Count);
                    Console.WriteLine("Dafabet Task wait all exception, line count " + lines.Count);
                }

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                return lines.ToArray();

            }
            catch (Exception e)
            {
                Log.Info($"ERROR Dafabet {e.Message} {e.StackTrace}");
            }

            return new LineDTO[] { };
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
                        var cookies = new CookieCollection();

                        //Get PHP Sessid
                        using (var client = new GetWebClient(host, cookies))
                        {
                            var res = client.DownloadString($"{Host}/eu/sports/");

                            if (client.CookieCollection.Count == 0) throw new Exception();

                            cookies.Add(client.CookieCollection);
                        }

                        ////Get ASP.Net sessionId for prices (используется в получении линий)
                        using (var client = new GetWebClient(host, cookies))
                        {
                            var u = $"{Host.Replace("www", "prices")}vender.aspx?lang=en_eu&iseuro=1&webskintype=1&act=hdpou&otype=1";

                            client.Headers["Referer"] = $"{Host}eu/sports/";

                            client.DownloadData(u);

                            cookies.Add(client.CookieCollection);
                        }

                        using (var client = new GetWebClient(host, cookies))
                        {
                            var u = $"{Host.Replace("www", "prices")}NewIndex?lang=en_eu&iseuro=1&webskintype=1&act=hdpou&otype=1";

                            client.Headers["Referer"] = $"{Host}eu/sports/";

                            client.DownloadData(u);

                            cookies.Add(client.CookieCollection);
                        }

                        ////Validate ASP.Net sessionId for prices (используется в получении линий)
                        using (var client = new GetWebClient(host, cookies))
                        {
                            var u = $"{Host.Replace("www", "prices")}EuroSite/Euro_index.aspx?lang=en_eu&iseuro=1";

                            client.Headers["Referer"] = $"{Host.Replace("www", "prices")}NewIndex?lang=en_eu&iseuro=1&webskintype=1&act=hdpou&otype=1";

                            client.DownloadData(u);

                            cookies.Add(client.CookieCollection);
                        }

                        using (var client = new GetWebClient(host, cookies))
                        {
                            client.Headers["Upgrade-Insecure-Requests"] = $"1";

                            client.DownloadString($"{Host.Replace("www", "prices")}EuroSite/Euro_Index.aspx?20110303");

                            cookies.Add(client.CookieCollection);
                        }

                        var result = new CookieContainer();
                        result.Add(cookies);

                        return result;

                    }
                    catch (Exception)
                    {
                        listToDelete.Add(host);
                        ConsoleExt.ConsoleWriteError($"Dafabet delete address {host.Address} listToDelete {listToDelete.Count}");
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
