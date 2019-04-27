using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bars.EAS.Utils.Extension;
using BM;

using BM.Web;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using Bars.EAS.IoC;
using Bars.EAS.Utils;
using BM.Core;
using BM.DTO;
using BM.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sbobet.Domain;
using HtmlAgilityPack;
using log4net;
using Sbobet.Models;
using PostWebClient = BM.Web.PostWebClient;

namespace Sbobet
{
    public class SBHelper
    {
        public static string Host;

        private readonly WebProxy _proxy;

        public static WebProxy Proxy;

        private static CachedArray<Dictionary<int, string>> _forbiddenTournamentsCached;

        public static CachedArray<Dictionary<int, string>> ForbiddenTournamentsCached => _forbiddenTournamentsCached ?? (_forbiddenTournamentsCached = new CachedArray<Dictionary<int, string>>(1000 * 60 * 5, UpdateTournamnets));

        private static readonly string[] StopWords =
        {
            "will advance to next",
            "set handicap",
            "After Game"
        };

        private static readonly ILog Log = LogManager.GetLogger("SBHelper");

        private static Dictionary<int, string> UpdateTournamnets()
        {
            var dict = new Dictionary<int, string>();

            var tournamentsUrl = Host + "/en/resource/e/euro-dynamic.js";
            string tournamentsResponce;

            using (var wc = new GetWebClient(Proxy))
            {
                tournamentsResponce = wc.DownloadString(tournamentsUrl);
            }

            var matchTour = Regex.Match(tournamentsResponce, "setElement[(]'tournaments',(?<json>.*?)[)];");
            var jsonTour = matchTour.Groups["json"].Value;

            jsonTour = DecodeJSString(jsonTour);

            var array = JsonConvert.DeserializeObject<JArray>(jsonTour);

            foreach (var tournament in array)
            {
                var name = tournament[1].ToString();

                if (!StopWords.Any(st => name.ContainsIgnoreCase(st))) continue;

                dict.Add((int)tournament[0], name);
            }

            return dict;
        }

        public static string DecodeJSString(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.Contains(@"\")) return s;

            var builder = new StringBuilder();
            var num = s.Length;
            var num2 = 0;
            while (num2 < num)
            {
                var ch = s[num2];
                if (ch != 0x5c)
                {
                    builder.Append(ch);
                }
                else if (num2 < (num - 5) && s[num2 + 1] == 0x75)
                {
                    var num3 = HexToInt(s[num2 + 2]);
                    var num4 = HexToInt(s[num2 + 3]);
                    var num5 = HexToInt(s[num2 + 4]);
                    var num6 = HexToInt(s[num2 + 5]);
                    if (num3 < 0 || num4 < 0 | num5 < 0 || num6 < 0)
                    {
                        builder.Append(ch);
                    }
                    else
                    {
                        ch = (char)((((num3 << 12) | (num4 << 8)) | (num5 << 4)) | num6);
                        num2 += 5;
                        builder.Append(ch);
                    }
                }
                else if (num2 < (num - 3) && s[num2 + 1] == 0x78)
                {
                    var num7 = HexToInt(s[num2 + 2]);
                    var num8 = HexToInt(s[num2 + 3]);
                    if (num7 < 0 || num8 < 0)
                    {
                        builder.Append(ch);
                    }
                    else
                    {
                        ch = (char)((num7 << 4) | num8);
                        num2 += 3;
                        builder.Append(ch);
                    }
                }
                else
                {
                    if (num2 < (num - 1))
                    {
                        var ch2 = s[num2 + 1];
                        if (ch2 == 0x5c)
                        {
                            builder.Append(@"\");
                            num2 += 1;
                        }
                        else if (ch2 == 110)
                        {
                            builder.Append("\n");
                            num2 += 1;
                        }
                        else if (ch2 == 0x74)
                        {
                            builder.Append("\t");
                            num2 += 1;
                        }
                    }
                    builder.Append(ch);
                }
                num2 += 1;
            }
            return builder.ToString();
        }

        public static string EncodeJSString(string sInput)
        {
            StringBuilder builder;
            string str;
            char ch;
            int num;
            builder = new StringBuilder(sInput);
            builder.Replace(@"\", @"\\");
            builder.Replace("\r", @"\r");
            builder.Replace("\n", @"\n");
            builder.Replace("\"", "\\\"");
            str = builder.ToString();
            builder = new StringBuilder();
            num = 0;
            while (num < str.Length)
            {
                ch = str[num];
                if (0x7f >= ch)
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.AppendFormat(@"\u{0:X4}", (int)ch);
                }
                num += 1;
            }
            return builder.ToString();
        }

        private static int HexToInt(char h)
        {
            if (h < 0x30 || h > 0x39)
            {
                if (h < 0x61 || h > 0x66)
                {
                    if (h < 0x41 || h > 0x46)
                    {
                        return -1;
                    }
                    return ((h - 0x41) + 10);
                }
                return ((h - 0x61) + 10);
            }
            return (h - 0x30);
        }

        public static string GetAltHttp(CookieCollection cookieCollection)
        {
            var url = cookieCollection.GetValue(AltHttp);

            return url;
        }

        public string ProcessSignIn(string login, string password, string host, out CookieCollection cookieCollection)
        {
            cookieCollection = new CookieCollection
            {
                new Cookie("lang", "en", "/", GetDomain(host))
            };

            string HidCK;
            string tag;
            string fingerprint;

            using (var wc = new GetWebClient(_proxy, cookieCollection))
            {
                var doc = new HtmlDocument();
                var result = wc.DownloadString(host);
                doc.LoadHtml(result);

                HidCK = doc.DocumentNode.SelectSingleNode("//input[@name='HidCK']").Attributes["value"].Value;
                tag = doc.DocumentNode.SelectSingleNode("//input[@name='tag']").Attributes["value"].Value;
                fingerprint = MD5.GetHashString(login); ;

                foreach (Cookie cookie in wc.CookieCollection)
                {
                    cookieCollection.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, GetDomain(host)));
                }
            }

            using (var wc = new PostWebClient(_proxy, cookieCollection))
            {
                var query = new NameValueCollection
                {
                    {  "id", login },
                    {  "password", password },
                    {  "type", "form" },
                    {  "lang", "en" },
                    {  "5", "1" },
                    {  "tzDiff", "5" },
                    {  "HidCK", HidCK },
                    {  "tag", tag },
                    {  "fingerprint", fingerprint },
                    {  "tk", $"2381,0,1,0,0,0,0,{DateTime.Now:yyyyMMdd},0,0,0,4" },
                };


                wc.Headers[HttpRequestHeader.Referer] = host + "betting.aspx";
                wc.Post(host + "web/public/process-sign-in.aspx", query);

                foreach (Cookie cookie in wc.CookieCollection)
                {
                    cookieCollection.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, GetDomain(host)));
                }

                return wc.ResponseHeaders["Location"].StartsWith("http")
                  ? wc.ResponseHeaders["Location"]
                  : $"{host}{wc.ResponseHeaders["Location"]}";
            }
        }

        public string GetLocation(string url, ref CookieCollection cookieCollection, string host)
        {
            using (var wc = new GetWebClient(_proxy, cookieCollection))
            {
                wc.DownloadData(url);

                cookieCollection.Add(wc.CookieCollection);

                if (wc.ResponseHeaders["Location"] == null) return string.Empty;

                return wc.ResponseHeaders["Location"].StartsWith("http")
                    ? wc.ResponseHeaders["Location"]
                    : $"{host}{wc.ResponseHeaders["Location"]}";
            }
        }

        public static string GetDomain(string host)
        {
            var domain = host.RegexStringValue("(?:http[s]?[:][/]+)(?:www.*?[.])?(?<value>.*?)(?:[/]|$)");
            return domain;
        }

        public static string GetValue(string data, string key)
        {
            var keyValue = Regex.Match(data, @"P.setToken[(]'site',new tilib_Token[(]\[(?<keys>.*?)\].*?\[(?<values>.*?)\][)][)];");
            var keys = keyValue.Groups["keys"].Value.Split(',');
            var values = keyValue.Groups["values"].Value.Split(',');

            for (var i = 0; i < keys.Length; i++)
            {
                if (keys[i].Trim('\'') == key)
                {
                    return values[i].Trim('\'');
                }
            }

            return null;
        }

        public static string UID = "uid";
        public static string AltHttp = "AltHttp";
        public static string TS = "ts";

        public static Dictionary<string, string> BuildQuery(BuyParams buyParams)
        {
            var query = new Dictionary<string, string>
            {
                 { "type", buyParams.type },
                 { "oid",  buyParams.oid.ToString() },
                 { "sel",  buyParams.sel },
                 { "price", buyParams.price.ToNormalFormat() },
                 { "act", buyParams.act },
                 { "ps", buyParams.ps.ToString() },
                 { "pid", buyParams.pid.ToString() },
                 { "eVersion", buyParams.eVersion.ToString() },
                 { "uid", buyParams.uid }
            };

            return query;
        }

        public static string BuildUrl(string url, BuyParams buyParams)
        {
            var query = BuildQuery(buyParams)
                .Select(x => "{0}={1}".FormatUsing(x.Key, x.Value));

            var queryString = string.Join("&", query.ToArray());
            return "{0}?{1}".FormatUsing(url, queryString);
        }

        public static BetValidatorData BuildValidator(BuyParams buyParams, string resp)
        {
            var json = resp.RegexStringValue(@"\['s',(?<value>.*?)\]");
            var array = JsonConvert.DeserializeObject<JArray>(json);

            foreach (var element in array)
            {
                if (element[0].ToObject<int>() != buyParams.oid)
                {
                    continue;
                }

                var betValidatorData = new BetValidatorData
                {
                    ActualCoeff = element[16].ToObject<decimal>(),
                    MinStake = element[17].ToObject<decimal>(),
                    MaxStake = element[18].ToObject<decimal>(),
                };

                return betValidatorData;
            }

            return null;
        }

        const string format = "{0}:{1}_{2}_{3}_{4}_{5}";

        internal static string BuildBuyUrl(LineDTO line, BuyParams buyParams, string altHttp)
        {
            var query = "{0}:{1}_{2}_{3}_{4}_{5}".FormatUsing(
                buyParams.type,
                buyParams.oid,
                buyParams.sel,
                1,
                buyParams.price.ToNormalFormat(),
                line.Price.ToNormalFormat());

            var url = "{0}/en/rdata/confirm-order?act=ord&bc={1}&orders={2}&eVersion=0&uid={3}".FormatUsing(
                altHttp,
                buyParams.bc,
                query,
                buyParams.uid);

            return url;
        }

        internal string WebGet(string url, CookieCollection cookie)
        {
            using (var wc = new GetWebClient(_proxy, cookie))
            {
                wc.BeforeRequest += request =>
                {
                    request.Accept = "*/*";
                    request.ContentType = "text/plain; charset=utf-8";
                    request.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.8,ru;q=0.6";
                    request.Referer = GetAltHttp(cookie) + "/euro/live-betting";
                };

                return wc.DownloadString(url);
            }
        }

        internal string WebPost(string url, CookieCollection cookie, string data)
        {
            using (var wc = new PostWebClient(_proxy, cookie))
            {
                wc.BeforeRequest += request =>
                {
                    request.Accept = "*/*";
                    request.ContentType = "text/plain; charset=utf-8";
                    request.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.8,ru;q=0.6";
                    request.Referer = GetAltHttp(cookie) + "/euro/live-betting";
                };

                return wc.UploadString(url, data);
            }
        }


        public static JArray GetJArray(string resp, string method)
        {
            Dictionary<string, string> groups;
            return GetJArray(resp, method, out groups);
        }

        public static JArray GetJArray(string resp, string method, out Dictionary<string, string> groups)
        {
            var pattern = GetPattern(method) ?? string.Empty;
            var rx = new Regex(pattern);
            var match = rx.Match(resp);

            if (!match.Success)
            {
                groups = null;
                return null;
            }

            groups = new Dictionary<string, string>();

            foreach (var groupName in rx.GetGroupNames())
            {
                groups[groupName] = match.Groups[groupName].Value;
            }

            return JArray.Parse(groups["data"]);
        }

        public static string GetPattern(string method)
        {
            switch (method)
            {
                case "onAddBetSlipCache":
                    return "onAddBetSlipCache[(](?<data>.*?)[)];";

                case "onReplace":
                    return @"onReplace[(](?<data>.*?),(?<draw>\d+)[)];";

                case "onShowReceipt":
                    return "onShowReceipt[(](?<data>.*?)[)];";

                case "onUpdateCacheData":
                    return "onUpdateCacheData[(](?<data>.*),(?<version>.*?)[)];";

                default: return null;
            }
        }

        public static bool CheckErrors(JToken jToken, out string message)
        {
            var alert = jToken[24].ToObject<bool>();
            var errorType = jToken[19].ToObject<int>();

            if (messages.TryGetValue("{0}{1}".FormatUsing(errorType, alert ? "" : "-alert"), out message))
            {
                return true;
            }

            if (!jToken[11].ToObject<bool>())
            {
                message = "Only live events is available";
                return true;
            }

            return false;
        }

        public static Dictionary<string, string> messages = new Dictionary<string, string>
        {
            { "2", "Odds has been closed." },
            { "3", "Please enter a valid stake." },
            { "4", "Your stake has exceeded the maximum bet." },
            { "4-alert", "Your stake has exceeded the maximum bet. System has adjusted your stake to the maximum bet." },
            { "5", "Odds are closed or temporary not available." },
            { "6", "Odds not available for betting." },
            { "7", "Sports excluded for betting." },
            { "8", "Insufficient bet credit." },
            { "9", "An error has occurred." },
            { "10", "Point has changed." },
            { "11", "Score has changed." },
            { "12", "This event is only available for members who signed up via SBOBET's website." },
            { "13", "Your stake is lower than the minimum bet." },
            { "13-alert", "Your stake is lower than the minimum bet. System has adjusted your stake to the minimum bet." },
            { "14", "Your betting amount has exceeded your betting budget limit." },
            { "15", "Your total bet has exceeded the maximum bet per match." },
            { "16", "Your account has a problem. Please check it." },
            { "17", "Event has been closed." },

            { "msg-max-bs", "You have exceeded the maximum number of selections." },
            { "msg-min-one-order", "Please select at least one bet selection." },
            { "msg-min-suborders", "Please make a minimum of 3 selections from different events to form a mix parlay bet." },
            { "msg-min-suborders-world-cup", "Please make the minimum selections to form a mix parlay bet:\n\nAt least 3 selections from different events; or\nAt least 2 selections from different World Cup events only" },
            { "msg-sign-in-only", "Please sign in before placing a bet. Register now, if you are not a SBOBET user yet." },
            { "place-bet", "Place Bet" },
            { "status1", "Running" },
            { "status2", "Waiting" }
        };

        public SBHelper(WebProxy proxy)
        {
            _proxy = proxy;
        }


    }
}
