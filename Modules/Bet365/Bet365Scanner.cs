using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bars.EAS.Utils;
using Bars.EAS.Utils.Extension;
using BM.Core;
using BM.DTO;
using BM.Web;
using Extreme.Net;
using Favbet;
using Favbet.Models.Line;
using mevoronin.RuCaptchaNETClient;
using Newtonsoft.Json;
using Scanner;
using Scanner.Helper;
using WebSocketSharp;

namespace Bet365
{
    public class Bet365Scanner : ScannerBase
    {
        static readonly object Lock = new object();

        public override string Name => "Favbet";

        public override string Host => "https://www.348365365.com/";

        public string WebSocketUrl = "wss://premws-pt2.365pushodds.com/zap/?uid=";

        public static Dictionary<WebProxy, CachedArray<CookieContainer>> CookieDictionary = new Dictionary<WebProxy, CachedArray<CookieContainer>>();

        //public static readonly List<string> ForbiddenTournaments = new List<string> { "statistics", "cross", "goal", "shot", "offside", "corner", "foul" };


        protected override LineDTO[] GetLiveLines()
        {
            var lines = new List<LineDTO>();


            //var randomProxy = ProxyList.PickRandom();

            //var c = CookieDictionary[randomProxy].GetData().GetAllCookies();

            //if (c["pstk"] == null)
            //{
            //    throw new Exception("null pstk cookie");
            //}

            //var sessionID = c["pstk"].Value;

            var r = new Random();

            string random16 = Math.Round(r.NextDouble() * 1e+2, 0).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + Math.Round(r.NextDouble() * 1e+14, 0).ToString(CultureInfo.InvariantCulture).PadLeft(14, '0');

            var ws = new WebSocket(WebSocketUrl + random16);

            ws.CustomHeaders = new Dictionary<string, string> {
                {"Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits"},
                {"Sec-WebSocket-Protocol", "zap-protocol-v1"},
                //{"Sec-WebSocket-Key", "4oe/l/gpfrk8/xtLwu8ufw=="},
                {"Sec-WebSocket-Version", "13"},
                {"Upgrade", "websocket"},
                {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36"},
                {"Host", "premws-pt2.365pushodds.com"},
                {"Accept-Encoding", "gzip, deflate, br"},
                {"Accept-Language", " ru-RU,ru; q = 0.9,en - US; q = 0.8,en; q = 0.7"}

            };




            //ws.SetProxy($"http://{randomProxy.Address.Host}:{randomProxy.Address.Port}", randomProxy.Credentials.GetCredential(randomProxy.Address, "").UserName, randomProxy.Credentials.GetCredential(randomProxy.Address, "").Password);

            ws.OnOpen += (sender, e) =>
            {

                Console.Write("on open");
            };


            ws.OnError += (sender, e) =>
            {
                Console.Write("OnError");
            };


            ws.OnClose += (sender, e) =>
            {
                Console.Write("OnClose");
            };

            ws.OnMessage += (sender, e) =>
            {
                Console.Write("OnMessage");
            };

            //    ws.OnMessage += (sender, e) =>
            //        Console.WriteLine("Laputa says: " + e.Data);

            ws.Connect();
            //ws.Send(sessionID);



            //    Console.ReadKey(true);


            return new LineDTO[] { };
        }



        //protected override void CheckDict()
        //{
        //    var listToDelete = new List<WebProxy>();

        //    foreach (var host in ProxyList)
        //    {
        //        CookieDictionary.Add(host, new CachedArray<CookieContainer>(1000 * 3600 * 3, () =>
        //        {
        //            try
        //            {
        //                var cc = new CookieContainer();

        //                using (var wc = new Extensions.WebClientEx(host, cc))
        //                {
        //                    wc.Headers["User-Agent"] = GetWebClient.DefaultUserAgent;

        //                    wc.DownloadString(Host+"en/");

        //                    cc.Add(wc.CookieContainer.GetAllCookies());
        //                }

        //                return cc;
        //            }
        //            catch (Exception e)
        //            {
        //                listToDelete.Add(host);
        //                ConsoleExt.ConsoleWriteError($"Bet365 delete address {host.Address} listToDelete {listToDelete.Count}");
        //            }

        //            return null;
        //        }));
        //    }

        //    var tasks = ProxyList.AsParallel().Select(host => Task.Factory.StartNew(state => CookieDictionary[host].GetData(), host)).ToArray();

        //    Task.WaitAll(tasks);

        //    foreach (var host in listToDelete)
        //    {
        //        CookieDictionary.Remove(host);
        //        ProxyList.Remove(host);
        //    }
        //}
    }
}
