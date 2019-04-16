using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
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
               
                var cookies = CookieDictionary[randomProxy].GetData();

                var result = new MatchDataResult();

                List<Game> games = new List<Game>();

                using (var client = new Extensions.WebClientEx(randomProxy, cookies))
                {
                    client.Headers["Content-Type"] = "application/x-www-form-urlencoded";

                    var response = client.UploadString($"https://ismart.dafabet.com/main/GetContributor", "isParlay=false&both=false");

                    var contributorResult = JsonConvert.DeserializeObject<BaseDataResult<List<Game>>>(response);

                     games = contributorResult.Data.Where(d => d.M0.L > 0).ToList();

                    //cookies.Add(client.ResponseCookies);
                }

             
                foreach (var game in games)
                {
                    using (var client = new Extensions.WebClientEx(randomProxy, cookies))
                    {
                        client.Headers["Content-Type"] = "application/x-www-form-urlencoded";

                        var l  = client.UploadString($"https://ismart.dafabet.com/Odds/ShowAllOdds",$"GameId={game.GameId}&DateType=l&BetTypeClass=more");

                        var t = JsonConvert.DeserializeObject<BaseDataResult<ShowAllOddData>>(l);

                        foreach (var leagueKeyValuePair in t.Data.LeagueN)
                        {
                            //убираем запрещенные чемпионаты
                            if (leagueKeyValuePair.Value.ContainsIgnoreCase(LeagueStopWords.ToArray())) continue;


                            var matches = new List<Match>();
                            var m = t.Data.NewMatch.Where(d => d.LeagueId == leagueKeyValuePair.Key).ToList();

                            foreach (var match in m)
                            {
                                var k = new Match();
                                k.HomeName = t.Data.TeamN.First(e => e.Key == match.TeamId1).Value;
                                k.AwayName = t.Data.TeamN.First(y => y.Key == match.TeamId2).Value;
                                k.IsLive = match.IsLive;
                                k.MatchId = match.MatchId;
                                k.MoreInfo = new MoreInfo()
                                {
                                    ScoreA = match.T1V,
                                    ScoreH = match.T2V
                                };

                                var l3 = client.UploadString($"https://ismart.dafabet.com/Odds/GetMarket", $"GameId={game.GameId}&DateType=l&BetTypeClass=OU&Matchid={match.MatchId}");
                                var t2 = JsonConvert.DeserializeObject<BaseDataResult<GetMarketData>>(l3);

                                foreach (var newOdd in t2.Data.Markets.NewOdds)
                                {

                                    var oddset = new OddSet();
                                    oddset.Bettype = newOdd.BetTypeId;
                                    oddset.OddsId = newOdd.MarketId;

                                    foreach (var selection in newOdd.Selections)
                                    {
                                        var sel = new Select();
                                        sel.Price = selection.Price;
                                        sel.Key = selection.SelId;
                                        sel.Point = newOdd.Line;
                                        oddset.sels.Add(sel);
                                    }

                                    k.oddset.Add(oddset);
                                }

                                matches.Add(k);
                            }

                            result.leagues.Add(new League()
                            {
                                LeagueName = leagueKeyValuePair.Value,
                                matches = matches,
                                SportName = game.Name
                            });
                        }
                    }
                }

                st.Stop();

                Log.Info("Dafabet after OddsManager/Standard request " + st.Elapsed);

                //var tf = new List<long>();
                //tf.AddRange(_linesDictionary.Where(lineDtose => !matchList.Contains(lineDtose.Key)).Select(l => l.Key));

                //удаляем устаревшие события
                //foreach (var lineDtose in tf)
                //    _linesDictionary.Remove(lineDtose);

                //добавляем новые события
                //foreach (var matchId in matchList)
                //{
                //    if (_linesDictionary.ContainsKey(matchId)) continue;

                //    _linesDictionary.Add(matchId, new EventUpdateObject(() =>
                //    {
                //        var random = ProxyList.PickRandom();
                //        using (var cl = new Extensions.WebClientEx(random, CookieDictionary[randomProxy].GetData()))
                //        {
                //            cl.Headers["X-Requested-With"] = "XMLHttpRequest";
                //            cl.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                //            cl.Headers["__VerfCode"] = cookies.GetAllCookies()["VerfCode"].Value;

                //            var res = cl.UploadString(new Uri($"{Host.Replace("www", "play")}OddsManager/Standard"), $"FixtureType=l&SportType=1&LDisplayMode=0&Scope=Match&IsParlay=false&MatchId={matchId}");
                //            var converter = new DafabetConverter();

                //            return converter.Convert(res, Name).ToList();
                //        }
                //    }));
                //}


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
                            client.Headers["Referer"] = $"https://m.dafabet.com/en/login?product=sports";

                            var r = client.DownloadString($"https://m.dafabet.com");

                            cookies.Add(client.CookieCollection);


                        }

                        string hash;
                        using (var client = new PostWebClient(host, cookies))
                        {
                            client.Headers["Referer"] = $"https://m.dafabet.com/en/login";
                            var requestParams = new NameValueCollection
                            {
                                {"username", "antmatveev81"},
                                {"password", "dbQXhM2bB"},
                            };

                            var r = client.Post<dynamic>($"https://m.dafabet.com/en/api/plugins/component/route/header_login/authenticate", requestParams);

                            hash = r.hash;

                            cookies.Add(client.CookieCollection);
                        }
                        string token;
                        using (var client = new PostWebClient(host, cookies))
                        {
                            client.Headers["Referer"] = $"https://m.dafabet.com/en/";
                       
                            var r = client.Post<dynamic>($"https://m.dafabet.com/en/api/plugins/module/route/pas_integration/updateToken?authenticated=true&hash={hash}");

                            token = r.token;

                            cookies.Add(client.CookieCollection);
                        }

                        using (var client = new GetWebClient(host, cookies))
                        {
                            client.Headers["Referer"] = $"https://m.dafabet.com/en/login?product=sports";

                            var r = client.DownloadString($"https://ismart.dafabet.com/Deposit_ProcessLogin.aspx?lang=en&st={token}&homeURL=https%3A%2F%2Fm.dafabet.com%2Fen&extendSessionURL=https%3A%2F%2Fm.dafabet.com%2Fen&OType=01&oddstype=1");

                            cookies.Add(client.CookieCollection);


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
