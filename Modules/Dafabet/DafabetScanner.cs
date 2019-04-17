using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Entities;
using BM.Web;
using Dafabet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scanner;
using Scanner.Helper;
using Extensions = Scanner.Extensions;

namespace Dafabet
{
    public class DafabetScanner : ScannerBase
    {
        public override string Name => "Dafabet";

        public override string Host => "https://www.dafabet.com/";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();
        private CookieCollection _authorizeCookies;
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

        private const string BASE_URL = "https://ismart.dafabet.com/";
        private static readonly string GET_MARKETS_URL = $"{BASE_URL}Odds/GetMarket";
        private static readonly string GET_ALL_ODS_URL = $"{BASE_URL}Odds/ShowAllOdds";
        private static readonly string GET_CONTRIBUTOR_URL = $"{BASE_URL}main/GetContributor";

        //private WebProxy randomProxy;

        protected override void UpdateLiveLines()
        {
            var st = new Stopwatch();

            st.Start();

            try
            {
                var
                randomProxy = ProxyList.PickRandom();
                var cookies = CookieDictionary[randomProxy].GetData();

                var result = new MatchDataResult();

                List<Game> games;

                using (var client = new Extensions.WebClientEx(randomProxy, cookies))
                {
                    client.Headers["Content-Type"] = "application/x-www-form-urlencoded";

                    var response = client.UploadString(GET_CONTRIBUTOR_URL, "isParlay=false&both=false");

                    var contributorResult = JsonConvert.DeserializeObject<BaseDataResult<List<Game>>>(response);

                    games = contributorResult.Data.Where(d => d.M0.L > 0).ToList();

                    _authorizeCookies.Add(client.ResponseCookies);
                }

                Parallel.ForEach(games, game =>
                {

                    //    foreach (var game in games)
                    //{
                    result.leagues.AddRange(GetLeagues(game));
                    //}
                });

                st.Stop();


                var converter = new DafabetConverter();

                var res = converter.Convert(result, Name);

                ActualLines = res;

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

            }
            catch (Exception e)
            {
                Log.Info($"ERROR Dafabet {e.Message} {e.StackTrace}");
            }
        }

        private List<League> GetLeagues(Game game)
        {
            var leagueList = new List<League>();

            try
            {
                var randomProxy = ProxyList.PickRandom();
                var cookies = CookieDictionary[randomProxy].GetData();

                using (var client = new Extensions.WebClientEx(randomProxy, cookies))
                {
                    client.Headers["Content-Type"] = "application/x-www-form-urlencoded";

                    var l = client.UploadString(GET_ALL_ODS_URL, $"GameId={game.GameId}&DateType=l&BetTypeClass=OU");

                    var t = JsonConvert.DeserializeObject<BaseDataResult<ShowAllOddData>>(l);

                    _authorizeCookies.Add(client.ResponseCookies);

                    //Log.Info("Dafabet matchList count " + t.Data.NewMatch.Count);


                    foreach (var league in t.Data.LeagueN)
                    {
                        //убираем запрещенные чемпионаты
                        if (league.Value.ToString().ContainsIgnoreCase(LeagueStopWords.ToArray())) continue;

                        leagueList.Add(new League
                        {
                            LeagueName = league.Value.ToString(),
                            matches = GetMatches(game, league, t.Data),
                            SportName = game.Name
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Dafabet GetLeagues {e.Message}");
            }

            return leagueList;
        }

        private List<Match> GetMatches(Game game, KeyValuePair<string, JToken> league, ShowAllOddData oddData)
        {
            var matches = new List<Match>();
            try
            {
                var matchList = oddData.NewMatch.Where(d => d.IsLive && d.LeagueId == long.Parse(league.Key)).ToList();

                Parallel.ForEach(matchList, match =>
                {
                    //foreach (var match in matchList)
                    //{


                    var m = new Match();
                    m.HomeName = oddData.TeamN[match.TeamId1.ToString()].ToString();
                    m.AwayName = oddData.TeamN[match.TeamId2.ToString()].ToString();
                    m.IsLive = match.IsLive;
                    m.MatchId = match.MatchId;

                    m.MoreInfo = new MoreInfo();
                    m.MoreInfo.ScoreH = match.T1V;
                    m.MoreInfo.ScoreA = match.T2V;

                    Thread.Sleep(500);
                    //m.oddset.AddRange(GetOddSet(game, match));

                    matches.Add(m);
                    //}
                });
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Dafabet GetMatches {e.Message}");
            }

            return matches;
        }

        //private List<OddSet> GetOddSet(Game game, NewMatch match)
        //{
        //    var listOdds = new List<OddSet>();

        //    var st = new Stopwatch();
        //    st.Start();

        //    try
        //    {
        //        var randomProxy = ProxyList.PickRandom();

        //        var cookies = CookieDictionary[randomProxy].GetData();

        //        using (var client = new Extensions.WebClientEx(randomProxy, cookies))
        //        {
        //            client.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        //            client.Headers["Accept"] = "application/json, text/javascript, */*; q=0.01";

        //            var responce = client.UploadString(GET_MARKETS_URL, $"GameId={game.GameId}&DateType=l&BetTypeClass=OU&Matchid={match.MatchId}");

        //            var marketsResult = JsonConvert.DeserializeObject<BaseDataResult<Markets>>(responce);

        //            _authorizeCookies.Add(client.ResponseCookies);

        //            if (marketsResult.Data.NewOdds.IsNull()) return listOdds;

        //            foreach (var newOdd in marketsResult.Data.NewOdds)
        //            {
        //                var oddset = new OddSet
        //                {
        //                    Bettype = newOdd.BetTypeId,
        //                    OddsId = newOdd.MarketId
        //                };

        //                foreach (var selection in newOdd.Selections)
        //                {
        //                    var sel = new Select
        //                    {
        //                        Price = decimal.Parse(selection.Value["Price"].ToString()),
        //                        Key = selection.Value["SelId"].ToString(),
        //                        Point = newOdd.Line
        //                    };

        //                    oddset.sels.Add(sel);
        //                }

        //                listOdds.Add(oddset);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Info($"ERROR Dafabet GetOddSet {e.Message}");
        //    }

        //    //Log.Info($"GetOddSet Elapsed {st.Elapsed}");

        //    return listOdds;
        //}

        protected override void CheckDict()
        {
            var listToDelete = new List<WebProxy>();

            ProxyList = ProxyList.Take(1).ToList();

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
                            client.DownloadString($"https://ismart.dafabet.com");

                            cookies.Add(client.CookieCollection);
                        }

                        result.Add(cookies);
                        result.Add(Authorize(host));

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

        private CookieCollection Authorize(WebProxy host)
        {
            if (_authorizeCookies != null) return _authorizeCookies;

            var cookies = new CookieCollection();

            using (var client = new GetWebClient(host, cookies))
            {
                client.Headers["Referer"] = $"https://m.dafabet.com/en/login?product=sports";

                client.DownloadString($"https://m.dafabet.com");

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

                client.DownloadString($"https://ismart.dafabet.com/Deposit_ProcessLogin.aspx?lang=en&st={token}&homeURL=https%3A%2F%2Fm.dafabet.com%2Fen&extendSessionURL=https%3A%2F%2Fm.dafabet.com%2Fen&OType=01&oddstype=1");

                cookies.Add(client.CookieCollection);
            }

            return _authorizeCookies = cookies;
        }

    }


}



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