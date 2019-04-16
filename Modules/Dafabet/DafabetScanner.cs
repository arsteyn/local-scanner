using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Entities;
using BM.Web;
using Dafabet.Models;
using Newtonsoft.Json;
using Scanner;
using Scanner.Helper;
using Match = System.Text.RegularExpressions.Match;

namespace Dafabet
{
    public class DafabetScanner : ScannerBase
    {
        public override string Name => "Dafabet";

        public override string Host => "https://www.dafabet.com/";

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

        protected override void UpdateLiveLines()
        {
            var randomProxy = ProxyList.PickRandom();

            var st = new Stopwatch();

            st.Start();

            try
            {
                var matchList = new List<long>();

                var cookies = CookieDictionary[randomProxy].GetData();

                using (var client = new Extensions.WebClientEx(randomProxy, cookies))
                {
                    client.Headers["X-Requested-With"] = "XMLHttpRequest";
                    client.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                    client.Headers["__VerfCode"] = cookies.GetAllCookies()["VerfCode"].Value;

                    var response = client.UploadString($"{Host.Replace("www", "play")}OddsManager/Standard", "FixtureType=l&SportType=1&LDisplayMode=0&Scope=MainMarket&IsParlay=false");

                    var t = JsonConvert.DeserializeObject<MatchDataResult>(response);

                    foreach (var league in t.leagues)
                    {
                        //убираем запрещенные чемпионаты
                        if (league.LeagueName.ContainsIgnoreCase(LeagueStopWords.ToArray())) continue;

                        matchList.AddRange(league.matches.Where(m => m.IsLive).Select(m => m.MatchId).ToList());
                    }
                }

                st.Stop();

                Log.Info("Dafabet after OddsManager/Standard request " + st.Elapsed);

                var tf = new List<long>();
                tf.AddRange(_linesDictionary.Where(lineDtose => !matchList.Contains(lineDtose.Key)).Select(l => l.Key));

                //удаляем устаревшие события
                foreach (var lineDtose in tf)
                    _linesDictionary.Remove(lineDtose);

                //добавляем новые события
                foreach (var matchId in matchList)
                {
                    if (_linesDictionary.ContainsKey(matchId)) continue;

                    _linesDictionary.Add(matchId, new EventUpdateObject(() =>
                    {
                        var random = ProxyList.PickRandom();
                        using (var cl = new Extensions.WebClientEx(random, CookieDictionary[randomProxy].GetData()))
                        {
                            cl.Headers["X-Requested-With"] = "XMLHttpRequest";
                            cl.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                            cl.Headers["__VerfCode"] = cookies.GetAllCookies()["VerfCode"].Value;

                            var res = cl.UploadString(new Uri($"{Host.Replace("www", "play")}OddsManager/Standard"), $"FixtureType=l&SportType=1&LDisplayMode=0&Scope=Match&IsParlay=false&MatchId={matchId}");
                            var converter = new DafabetConverter();

                            

                            return converter.Convert(res, Name).ToList();
                        }
                    }));
                }


                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                //ActualLines = lines.ToArray();

            }
            catch (Exception e)
            {
                Log.Info($"ERROR Dafabet {e.Message} {e.StackTrace}");
            }
        }

        protected override void CheckDict()
        {
            var listToDelete = new List<WebProxy>();

            foreach (var host in ProxyList)
            {
                CookieDictionary.Add(host, new CachedArray<CookieContainer>(1000 * 60 * 10, () =>
                {
                    try
                    {
                        var cookies = new CookieCollection();

                        var result = new CookieContainer();

                        using (var client = new GetWebClient(host, cookies))
                        {
                            client.Headers["Referer"] = $"{Host.Replace("www", "play")}NewIndex?act=hdpou&webskintype=2";

                            var r = client.DownloadString($"{Host.Replace("www", "play")}onebook?act=hdpou");

                            cookies.Add(client.CookieCollection);

                            var parser = new HtmlParser();

                            var results = parser.Parse(r);

                            var verfCode = results.QuerySelector("input[name=__RequestVerificationToken]").GetAttribute("value");

                            cookies.Add(new Cookie("VerfCode", verfCode, "/", new Uri(Host).Host));
                        }

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
