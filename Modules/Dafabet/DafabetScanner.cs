using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading;
using Bars.EAS.Utils;
using Bars.EAS.Utils.Extension;
using BM.Web;
using Dafabet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quobject.Collections.Immutable;
using Quobject.EngineIoClientDotNet.Client.Transports;
using Quobject.SocketIoClientDotNet.Client;
using Scanner;

namespace Dafabet
{
    public class DafabetScanner : ScannerBase
    {
        readonly object _sync = new object();

        private Socket _socket;

        private ConcurrentDictionary<long, Match> _matches;

        public override string Name => "Dafabet";

        public override string Host => $"https://{_host}/";

        private string _host;

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
            "(ET)",
            "team",
            "advance",
            "round",
            "winner"
        };

        public DafabetScanner()
        {
            _matches = new ConcurrentDictionary<long, Match>();
            _subscribedMarkets = new List<long>();

            InitConnection();
        }

        Timer _subscribeAllMarketsTimer;
        private string _token;
        private string _id;
        private int _exp;

        private void InitConnection()
        {
            GetToken();

            var gid = Guid.NewGuid().ToString("N").Substring(0, 16);
            var options = new IO.Options
            {
                QueryString = $"token={_token}&rid={new Random().Next(0, 2)}&id={_id}&gid={gid}",
                AutoConnect = true,
                ForceNew = true,
                Transports = ImmutableList<string>.Empty/*.Add(Polling.NAME)*/.Add(WebSocket.NAME),
                Reconnection = true,
                ReconnectionDelay = 1000,
                ReconnectionDelayMax = 5000,
                ReconnectionAttempts = int.MaxValue
            };

            _socket = IO.Socket(Host, options);

            SubscribeOnEvents();

            //var tm2 = new TimerCallback(state => SubscribeAllMarkets());

            //_subscribeAllMarketsTimer = new Timer(tm2, null, 10 * 1000, 10 * 1000);
        }

        List<long> _subscribedMarkets;

        //private void SubscribeAllMarkets()
        //{
        //    var ids = _matches.Values.Where(m => m.mc > 0).Select(m => m.matchid).ToList();

        //    if (!ids.Any() || ids.All(evId => _subscribedMarkets.Contains(evId))) return;


        //    //var cs = new List<string> { "c" + c };

        //    //dynamic off = JToken.FromObject(cs); ;
        //    //_socket.Emit("unsubscribe", off);


        //    foreach (var id in ids)
        //    {
        //        var objects = new List<SubscribeObject>();

        //        var o = new SubscribeObject
        //        {
        //            id = "c" + c++,
        //            rev = 0,
        //            condition = new ConditionMatch()
        //            {
        //                marketid = "L",
        //                sorting = "n",
        //                more = 1,
        //                matchid = id,
        //                timestamp = 0
        //            }
        //        };

        //        objects.Add(o);

        //        dynamic obj = JToken.FromObject(objects); ;

        //        _socket.Emit("subscribe", "odds", obj);

        //        Log.Info($"{Name} SubscribeAllMarkets {id}");
        //    }


        //    _subscribedMarkets = ids;
        //}

        private void GetToken()
        {
            var cookies = new CookieCollection();

            var refferer = "https://prices.sportdafa.net/NewIndex?lang=en&iseuro=0&webskintype=3&act=hdpou&otype=4";
            string location;
            string gid;

            using (var client = new GetWebClient())
            {
                client.Referer = refferer;
                client.DownloadData("https://prices.sportdafa.net/vender.aspx?lang=en&iseuro=0&webskintype=3&act=hdpou&otype=4");
                cookies.Add(client.CookieCollection);
            }

            using (var client = new GetWebClient(cookies))
            {
                client.Referer = refferer;
                gid = client.DownloadResult<dynamic>("https://prices.sportdafa.net/NewIndex/GetAppConfig").GUID;
                cookies.Add(client.CookieCollection);
            }

            using (var client = new GetWebClient(cookies))
            {
                client.Referer = refferer;
                client.DownloadData($"https://prices.sportdafa.net/EntryIndex/OpenSports?lang=en&iseuro=0&act=hdpou&otype=4&webskintype=3&gid={gid}");
                location = client.ResponseHeaders["Location"];
            }

            using (var client = new GetWebClient(cookies))
            {
                client.Referer = refferer;
                client.DownloadData(location);
                cookies.Add(client.CookieCollection);
            }

            using (var client = new GetWebClient(cookies))
            {
                client.Referer = refferer;
                var ss = client.DownloadString("https://fbw.sportdafa.net/Sports/1/?mode=m0&market=L");
                _token = ss.RegexStringValue("\"tk\":\"(?<value>.*?)\"");
                _host = ss.RegexStringValue("p: \"(?<value>.*?)\"");
                _id = ss.RegexStringValue("id = \"(?<value>.*?)\"");
            }
        }

        private int c = 0;
        private void SubscribeOnEvents()
        {
            _socket.On(Socket.EVENT_CONNECT, () =>
            {
                try
                {
                    Log.Info($"{Name} EVENT_CONNECT");

                    var objects = new List<SubscribeObject>();

                    var o = new SubscribeObject
                    {
                        id = "c" + c++,
                        rev = 0,
                        condition = new Condition
                        {
                            marketid = "L",
                            sorting = "n",
                            sporttype = 1 //Football
                        }
                    };

                    objects.Add(o);

                    dynamic obj = JToken.FromObject(objects); ;

                    _socket.Emit("subscribe", "odds", obj);
                }
                catch (Exception e)
                {
                    Log.Info($"ERROR {Name} Exception {JsonConvert.SerializeObject(e)}");
                }
            });

            _socket.On("r", (data) =>
            {
                //Log.Info($" {Name} EVENT r");

                var s = data as object[];
                var f = s[0] as JArray;

                lock (_sync) InitMatchList(f[2]);
            });

            _socket.On("p", (data) =>
            {
                //Log.Info($" {Name} EVENT p");

                var s = data as object[];
                var f = s[0] as JArray;

                lock (_sync) UpdateMatchList(f[1]);

            });

            _socket.On(Socket.EVENT_DISCONNECT, (data) =>
            {
                Log.Info($"ERROR  {Name} EVENT_DISCONNECT {JsonConvert.SerializeObject(data)}");

                lock (_sync) _matches.Clear();
                //lock (_sync) _subscribedMarkets.Clear();

                _socket.Off();
                _socket.Close();

                InitConnection();
            });

            _socket.On(Socket.EVENT_ERROR, (data) =>
            {
                Log.Info($"ERROR  {Name} EVENT_ERROR ");
            });

            _socket.On(Socket.EVENT_RECONNECT, (attemptNumber) =>
            {
                Log.Info($"ERROR {Name} EVENT_RECONNECT {JsonConvert.SerializeObject(attemptNumber)}");
            });

            _socket.On(Socket.EVENT_RECONNECTING, (attemptNumber) =>
            {
                Log.Info($"ERROR {Name} EVENT_RECONNECTING {JsonConvert.SerializeObject(attemptNumber)}");
            });

            _socket.On(Socket.EVENT_RECONNECT_ATTEMPT, (attemptNumber) =>
            {
                Log.Info($"ERROR {Name} EVENT_RECONNECT_ATTEMPT {JsonConvert.SerializeObject(attemptNumber)}");
            });
        }


        private void UpdateMatchList(object data)
        {
            var e = data as JArray;

            foreach (var obj in e)
            {
                var type = obj["type"].ToString();
                switch (type)
                {

                    case "do":

                        //Log.Info($"{Name} EVENT do");

                        var oddsid = obj["oddsid"].ToLong();

                        var match = _matches.Values.FirstOrDefault(m => m.oddset.ContainsKey(oddsid));

                        if (match != null)
                        {
                            //Log.Info($"{Name} match.oddset.Remove {match.oddset.Count} {oddsid} {match.oddset[oddsid].bettype}");
                            match.oddset.Remove(oddsid);
                            //Log.Info($"{Name} match.oddset.Remove {_matches[match.matchid].oddset.Count}");
                        }

                        break;

                    case "dm":
                        //Log.Info($"{Name} EVENT dm");

                        var matchid = obj["matchid"].ToLong();

                        _matches.TryRemove(matchid, out _);

                        break;
                    case "m":
                        try
                        {
                            //update match
                            var mt = obj.ToObject<JObject>();

                            if (_matches.TryGetValue(mt["matchid"].ToLong(), out var ma))
                            {
                                if (mt["liveawayscore"] != null) ma.liveawayscore = mt["liveawayscore"].ToInt();
                                if (mt["livehomescore"] != null) ma.livehomescore = mt["livehomescore"].ToInt();
                                if (mt["eventstatus"] != null) ma.eventstatus = mt["eventstatus"].ToString();
                            }
                            else
                            {
                                var m = obj.ToObject<Match>();

                                if (m == null ) continue;

                                if (LeagueStopWords.Any(w => m.leaguenameen.ContainsIgnoreCase(w))) break;

                                if (string.IsNullOrEmpty(m.hteamnameen) || string.IsNullOrEmpty(m.ateamnameen))
                                {
                                    Log.Info($"Dafabet ERROR initmatch empty team {JsonConvert.SerializeObject(obj)}");
                                    continue;
                                }

                                _matches.GetOrAdd(m.matchid, m);
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Info($"{Name} update match ERROR {JsonConvert.SerializeObject(exception)}");
                        }
                        break;

                    case "o":

                        try
                        {
                            var oddsets = _matches.Values.SelectMany(m => m.oddset).ToDictionary(m => m.Key, pair => pair.Value);

                            var o = obj.ToObject<JObject>();

                            if (oddsets.TryGetValue(o["oddsid"].ToLong(), out var oddSet))
                            {
                                if (obj["odds1a"] != null) oddSet.odds1a = obj["odds1a"].ToDecimal();
                                if (obj["odds2a"] != null) oddSet.odds2a = obj["odds2a"].ToDecimal();

                                if (obj["oddsstatus"] != null) oddSet.oddsstatus = obj["oddsstatus"].ToString();

                                if (obj["com1"] != null) oddSet.com1 = obj["com1"].ToDecimal();
                                if (obj["com2"] != null) oddSet.com2 = obj["com2"].ToDecimal();
                                if (obj["comx"] != null) oddSet.comx = obj["comx"].ToDecimal();

                                if (obj["hdp1"] != null) oddSet.hdp1 = obj["hdp1"].ToDecimal();
                                if (obj["hdp2"] != null) oddSet.hdp2 = obj["hdp2"].ToDecimal();

                                if (obj["cs00"] != null) oddSet.cs00 = obj["cs00"].ToDecimal();
                                if (obj["cs10"] != null) oddSet.cs10 = obj["cs10"].ToDecimal();
                                if (obj["cs20"] != null) oddSet.cs20 = obj["cs20"].ToDecimal();
                            }
                            else
                            {
                                var o1 = obj.ToObject<OddSet>();

                                if (o1 == null) continue;

                                if (_matches.ContainsKey(o1.matchid))
                                    _matches[o1.matchid].oddset.Add(o1.oddsid, o1);
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Info($"{Name} update oddset ERROR {JsonConvert.SerializeObject(exception)}");
                        }

                        break;
                }
            }
        }

        public static bool IsPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }

        private void InitMatchList(object data)
        {
            var e = data as JArray;

            foreach (var obj in e)
            {
                var type = obj["type"].ToString();

                switch (type)
                {
                    case "reset":

                        ///TODO: ???????????????
                        //_matches.Clear();

                        break;

                    //new match
                    case "m":

                        var mt = obj.ToObject<JObject>();

                        if (_matches.TryGetValue(mt["matchid"].ToLong(), out var ma))
                        {
                            if (mt["liveawayscore"] != null) ma.liveawayscore = mt["liveawayscore"].ToInt();
                            if (mt["livehomescore"] != null) ma.livehomescore = mt["livehomescore"].ToInt();
                            if (mt["eventstatus"] != null) ma.eventstatus = mt["eventstatus"].ToString();
                        }
                        else
                        {
                            var m = obj.ToObject<Match>();

                            if (m == null) continue;

                            if (LeagueStopWords.Any(w => m.leaguenameen.ContainsIgnoreCase(w))) break;

                            if (string.IsNullOrEmpty(m.hteamnameen) || string.IsNullOrEmpty(m.ateamnameen))
                            {
                                Log.Info($"Dafabet ERROR initmatch empty team {JsonConvert.SerializeObject(obj)}");
                                continue;
                            }

                            _matches.GetOrAdd(m.matchid, m);
                        }

                        break;

                    //new oddset
                    case "o":

                        var o = obj.ToObject<OddSet>();

                        if (o == null) continue;

                        //if (o.enable != 1) continue;

                        if (_matches.ContainsKey(o.matchid) && !_matches[o.matchid].oddset.ContainsKey(o.oddsid))
                            //Log.Info($"{Name} new oddset {o.oddsid} {o.bettype}");
                            _matches[o.matchid].oddset.Add(o.oddsid, o);

                        break;
                }
            }
        }

        private DateTime? _showLog;

        protected override void UpdateLiveLines()
        {
            try
            {
                var scanner = new DafabetConverter();

                string serializedMatches;
                lock (_sync) serializedMatches = JsonConvert.SerializeObject(_matches.Values);
                var copy = JsonConvert.DeserializeObject<List<Match>>(serializedMatches);

                var lines = scanner.Convert(copy, Name);


                if (_showLog == null || (DateTime.Now - _showLog).Value.Seconds > 2)
                {
                    //ConsoleExt.ConsoleWrite(Name, 0, _matches.Values.SelectMany(m => m.oddset).Count(), new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                    ConsoleExt.ConsoleWrite(Name, 0, lines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));
                    _showLog = DateTime.Now;
                }

                ActualLines = lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info($"ERROR {Name} {e.Message} {e.StackTrace}");
            }
        }

    }

    internal class SubscribeObject
    {
        public string id { get; set; }
        public int rev { get; set; }
        public ICondition condition { get; set; }
    }

    internal class Condition : ICondition
    {
        public string marketid { get; set; }
        public string sorting { get; set; }
        public int sporttype { get; set; }

    }

    internal class ConditionMatch : ICondition
    {
        public string marketid { get; set; }
        public string sorting { get; set; }
        public int more { get; set; }
        public long matchid { get; set; }
        public int timestamp { get; set; }
    }

    internal interface ICondition
    {
    }
}
