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
                var randomProxy = ProxyList.PickRandom();

                var cookies = CookieDictionary[randomProxy].GetData();

                var resultWithoudOddset = new MatchDataResult();

                List<Game> games;

                using (var client = new Extensions.WebClientEx(randomProxy, cookies))
                {
                    client.Headers["Content-Type"] = "application/x-www-form-urlencoded";

                    var response = client.UploadString(GET_CONTRIBUTOR_URL, "isParlay=false&both=false");

                    var contributorResult = JsonConvert.DeserializeObject<BaseDataResult<List<Game>>>(response);

                    games = contributorResult.Data.Where(d => d.M0.L > 0).ToList();
                }

                //Parallel.ForEach(games, game => resultWithoudOddset.leagues.AddRange(GetLeagues(game)));



                foreach (var game in games)
                {
                    //Thread.Sleep(1000);
                    resultWithoudOddset.leagues.AddRange(GetLeagues(game));
                }

                st.Stop();

                var matchList = resultWithoudOddset.leagues.SelectMany(l => l.matches).Select(m => m.MatchId).ToList();

                //var oldMatches = _linesDictionary.Where(eventUpdateObject => !matchList.Contains(eventUpdateObject.Key)).Select(l => l.Key).ToList();

                //удаляем устаревшие события
                //foreach (var oldMatchId in oldMatches)
                //    _linesDictionary.Remove(oldMatchId);

                _linesDictionary.Clear();

                var newMatches = matchList/*.Where(matchId => !_linesDictionary.ContainsKey(matchId)).ToList()*/;

                var chunksNewMatch = splitList(newMatches, newMatches.Count / ProxyList.Count).ToList();

                if (chunksNewMatch.Count > ProxyList.Count)
                {
                    chunksNewMatch[chunksNewMatch.Count - 2].AddRange(chunksNewMatch[chunksNewMatch.Count - 1]);
                    chunksNewMatch.RemoveAt(chunksNewMatch.Count - 1);
                }

                Parallel.ForEach(chunksNewMatch.Take(ProxyList.Count), newMatchListChunk =>
                {
                    var i = chunksNewMatch.FindIndex(a => a == newMatchListChunk);
                    var proxyForOddScrape = ProxyList[i];

                    //добавляем новые события
                    foreach (var matchId in newMatchListChunk)
                    {
                        var res = new MatchDataResult();

                        var id = matchId;
                        var league = resultWithoudOddset.leagues.First(l => l.matches.Any(m => m.MatchId == id));

                        var newLeague = new League
                        {
                            LeagueName = league.LeagueName,
                            SportName = league.SportName,
                            GameId = league.GameId
                        };

                        if (newLeague.SportName != "Soccer")
                            continue;

                        _linesDictionary.Add(matchId, new EventUpdateObject(() =>
                        {
                            var match = league.matches.First(m => m.MatchId == matchId);

                            match.oddset = GetOddSet(newLeague.GameId, match.MatchId, proxyForOddScrape);

                            newLeague.matches = new List<Match> { match };

                            res.leagues.Add(newLeague);

                            var converter = new DafabetConverter();

                            return converter.Convert(res, Name).ToList();
                        }));
                    }
                });

                Log.Info("Dafabet _linesDictionary count " + _linesDictionary.Count);

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, ActualLines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

            }
            catch (Exception e)
            {
                Log.Info($"ERROR Dafabet {e.Message} {e.StackTrace}");
            }
        }

        public static IEnumerable<List<T>> splitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
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

                    foreach (var league in t.Data.LeagueN)
                    {
                        //убираем запрещенные чемпионаты
                        if (league.Value.ToString().ContainsIgnoreCase(LeagueStopWords.ToArray())) continue;

                        leagueList.Add(new League
                        {
                            LeagueName = league.Value.ToString(),
                            matches = GetMatches(league, t.Data),
                            SportName = game.Name,
                            GameId = game.GameId
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

        private List<Match> GetMatches(KeyValuePair<string, JToken> league, ShowAllOddData oddData)
        {
            var matches = new List<Match>();
            try
            {
                var matchList = oddData.NewMatch.Where(d => d.IsLive && d.LeagueId == long.Parse(league.Key)).ToList();

                matches.AddRange(matchList.Select(match => new Match
                {
                    HomeName = oddData.TeamN[match.TeamId1.ToString()].ToString(),
                    AwayName = oddData.TeamN[match.TeamId2.ToString()].ToString(),
                    IsLive = match.IsLive,
                    MatchId = match.MatchId,
                    MoreInfo = new MoreInfo
                    {
                        ScoreH = match.T1V,
                        ScoreA = match.T2V
                    }
                }));
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Dafabet GetMatches {e.Message}");
            }

            return matches;
        }

        private List<OddSet> GetOddSet(int gameId, long matchId, WebProxy randomProxy)
        {
            var listOdds = new List<OddSet>();

            try
            {
                var cookies = CookieDictionary[randomProxy].GetData();

                using (var client = new Extensions.WebClientEx(randomProxy, cookies))
                {
                    client.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                    client.Headers["Accept"] = "application/json, text/javascript, */*; q=0.01";

                    var response = client.UploadString(GET_MARKETS_URL, $"GameId={gameId}&DateType=l&BetTypeClass=OU&Matchid={matchId}");

                    var marketsResult = JsonConvert.DeserializeObject<BaseDataResult<Markets>>(response);

                    if (marketsResult.Data.NewOdds.IsNull()) return listOdds;

                    foreach (var newOdd in marketsResult.Data.NewOdds)
                    {
                        var oddset = new OddSet
                        {
                            Bettype = newOdd.BetTypeId,
                            OddsId = newOdd.MarketId
                        };

                        foreach (var selection in newOdd.Selections)
                        {
                            var sel = new Select
                            {
                                Price = decimal.Parse(selection.Value["Price"].ToString()),
                                Key = selection.Value["SelId"].ToString(),
                                Point = newOdd.Line
                            };

                            oddset.sels.Add(sel);
                        }

                        listOdds.Add(oddset);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Dafabet GetOddSet {e.Message}");
            }

            return listOdds;
        }

        protected override void CheckDict()
        {
            var listToDelete = new List<WebProxy>();

            foreach (var account in _accounts)
            {
                foreach (var host in ProxyList)
                {
                    if (CookieDictionary.ContainsKey(host)) continue;

                    CookieDictionary.Add(host, new CachedArray<CookieContainer>(1000 * 60 * 10, () =>
                    {
                        try
                        {

                            var result = new CookieContainer();

                            result.Add(Authorize(host, account.Key, account.Value));

                            using (var client = new Extensions.WebClientEx(host, result))
                            {
                                client.Headers["Content-Type"] = "application/x-www-form-urlencoded";

                                var response = client.UploadString(GET_CONTRIBUTOR_URL, "isParlay=false&both=false");

                                JsonConvert.DeserializeObject<BaseDataResult<List<Game>>>(response);
                            }

                            return result;
                        }
                        catch (Exception)
                        {
                            listToDelete.Add(host);

                            ConsoleExt.ConsoleWriteError($"Dafabet delete address {host.Address} listToDelete {listToDelete.Count}");
                        }

                        return null;
                    }));

                    if (CookieDictionary[host].GetData() != null) break;

                    CookieDictionary.Remove(host);
                }
            }

            foreach (var host in ProxyList.OrderBy(p => Guid.NewGuid()))
            {
                if (!CookieDictionary.ContainsKey(host)) ProxyList.Remove(host);
            }


            var newProxy = new List<WebProxy>();

            foreach (var webProxy in ProxyList)
            {
                newProxy.Add(webProxy);
                newProxy.Add(webProxy);
                newProxy.Add(webProxy);
                newProxy.Add(webProxy);
            }

            ProxyList.AddRange(newProxy);
        }



        private CookieCollection Authorize(WebProxy host, string login, string password)
        {
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
                    {"username", login},
                    {"password", password},
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

            return cookies;
        }

        readonly Dictionary<string, string> _accounts = new Dictionary<string, string>()
        {
            { "antmatveev81", "dbQXhM2bB"},
            { "graudinagnt0", "sPDg52wQ"},
            { "renshar82", "2T1E5f0t"},
            { "romvitov82", "u56ENHy3"},
            { "inatmn93", "sPDg54wQ"},
            { "lexvasilev79", "72YxfB8f"},
            { "sergejromas97", "N903uKgk"},
            { "egoralikin998", "d4NAZ2D5"},
            { "dmitijkilin99", "CXaasW5h"},
            { "alexeyalex585", "73bR8u7q"},
            { "chelakdilsh", "fkjHfJ8"},
            { "vzakiev24", "4zSsrzsR"},
            { "nabokova85", "3zSsrzsR"},
            { "alivnov86", "8x65U442"},
        };

    }


}
