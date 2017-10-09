using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Web;
using Extreme.Net;
using Favbet.Models.Line;
using Newtonsoft.Json;
using Scanner;

namespace Favbet
{
    public class FavBetScanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "Favbet";

        public override string Host => "https://www.favbet.com/";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();

        private CookieCollection PassCloudFlare(WebProxy proxy)
        {
            var cookieCollection = new CookieCollection();


            #region Cloudflare wait 5 sec

            var cookies = CloudFlareNet.CloudFlareNet.GetCloudflareCookies(Host + "en/bets/", ExWebClient.DefaultUserAgent, new HttpProxyClient(proxy.Address.Host, proxy.Address.Port, proxy.Credentials.GetCredential(proxy.Address, "").UserName, proxy.Credentials.GetCredential(proxy.Address, "").Password));

            if (cookies == null || !cookies.Any()) throw new Exception();

            foreach (var cookie in cookies)
                cookieCollection.Add(new Cookie(cookie.Key, cookie.Value, "/", Domain));

            cookieCollection.Add(new Cookie("LANG", "en") { Domain = Domain });

            return cookieCollection;

            #endregion
        }

        private List<LineDTO> lines;

        protected override LineDTO[] GetLiveLines()
        {
            lines = new List<LineDTO>();

            var randomProxy = ProxyList.PickRandom();

            try
            {
                string response;
                using (var wc = new Extensions.WebClientEx(randomProxy, CookieDictionary[randomProxy].GetData()))
                {
                    wc.Headers["User-Agent"] = ExWebClient.DefaultUserAgent;

                    response = wc.DownloadString($"{Host}live/markets/");
                }

                var sports = JsonConvert.DeserializeObject<Market>(response).Sports;

                var tasks = new List<Task>();

                foreach (var tournament in sports.SelectMany(sport => sport.Tournaments))
                {
                    //убираем чемпионаты 
                    if (tournament.TournamentName.ContainsIgnoreCase("statistics", "crossbar", "goalpost", "fouls", "corners", "offsides", "shot"))
                        continue;

                    tasks.AddRange(tournament.Games.AsParallel().WithDegreeOfParallelism(4).Select(gameId =>
                        Task.Factory.StartNew(state => ParseGame(gameId, tournament), gameId)));
                }

                try
                {
                    Task.WaitAll(tasks.ToArray(), 10000);
                }
                catch (Exception e)
                {
                    Log.Info("FavBet Task wait all exception, line count " + lines.Count);
                    Console.WriteLine("FavBet Task wait all exception, line count " + lines.Count);
                }

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                return lines.ToArray();

            }
            catch (Exception e)
            {
                Log.Info($"ERROR FB {e.Message} {e.StackTrace}");
                //Console.WriteLine($"ERROR FB {e.Message} {e.StackTrace}");
            }

            return new LineDTO[] { };
        }


        private void ParseGame(Game gameId, Tournament tournament)
        {
            try
            {
                var converter = new FavBetConverter();

                var random = ProxyList.PickRandom();

                var game = ConverterHelper.GetFullGame(gameId.Id, random, CookieDictionary[random].GetData(), Host);

                if (game == null) return;

                game.TryInitProperties(converter.TeamRegex, converter.ScoreRegex);

                var lineGame = converter.CreateLineWithoutEvents(game, Host, Name);

                if (lineGame == null) return;

                var events = converter.AddEventsToLine(lineGame, game, tournament.TournamentName);

                lock (Lock) lines.AddRange(events);
            }
            catch (Exception e)
            {
                Log.Info("FB Parse event exception " + e.Message);
            }
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
                        var cc = new CookieContainer();

                        Console.WriteLine($"Favbet check address {host.Address}");

                        cc.Add(PassCloudFlare(host));

                        using (var wc = new Extensions.WebClientEx(host, cc))
                        {
                            wc.Headers["User-Agent"] = ExWebClient.DefaultUserAgent;

                            wc.DownloadString($"{Host}live/markets/");
                        }

                        return cc;
                    }
                    catch (Exception e)
                    {
                        listToDelete.Add(host);
                        Console.WriteLine($"Favbet delete address {host.Address} listToDelete {listToDelete.Count}");
                    }

                    return null;
                }));
            }


            //проверяем работу хоста
            //Parallel.ForEach(ProxyList, host => CookieDictionary[host].GetData());


            var tasks = ProxyList.AsParallel().Select(host => Task.Factory.StartNew(state => CookieDictionary[host].GetData(), host)).ToArray();
            Task.WaitAll(tasks.ToArray());

            foreach (var host in listToDelete)
            {
                CookieDictionary.Remove(host);
                ProxyList.Remove(host);
            }

        }
    }
}
