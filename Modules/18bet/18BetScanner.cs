using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Bars.EAS.Utils.Extension;
using Bet18.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quobject.Collections.Immutable;
using Quobject.EngineIoClientDotNet.Client.Transports;
using Quobject.SocketIoClientDotNet.Client;
using Scanner;

namespace Bet18
{
    public class Bet18Scanner : ScannerBase
    {
        readonly object _sync = new object();

        private Socket _socket;
        private readonly Dictionary<string, string> _rooms;

        private readonly ConcurrentDictionary<long, Event> _events;


        public override string Name => "Bet18";

        public sealed override string Host => "https://stream.89pin.net:3001";

        public Bet18Scanner()
        {
            _rooms = new Dictionary<string, string>();
            _events = new ConcurrentDictionary<long, Event>();
            _subscribedMarkets = new List<string>();

            InitConnection();

            var tm = new TimerCallback(a =>
            {
                GetToken();

                _socket.Emit("refresh", _token);

                Log.Info($"Bet18 refresh token {_token} {_exp}");

            });

            _refreshTokenTimer = new Timer(tm, null, _exp * 1000, _exp * 1000);

            var tm2 = new TimerCallback(state => SubscribeAllMarkets());

            _subscribeAllMarketsTimer = new Timer(tm2, null, 10 * 1000, 10 * 1000);
        }

        Timer _refreshTokenTimer;
        Timer _subscribeAllMarketsTimer;
        List<string> _subscribedMarkets;
        private string _token;
        private int _exp;

        private void InitConnection()
        {
            try
            {
                GetToken();

                var options = new IO.Options
                {
                    QueryString = $"token={_token}",
                    AutoConnect = true,
                    ForceNew = true,
                    Reconnection = true,
                    ReconnectionDelay = 1000,
                    ReconnectionDelayMax = 5000,
                    ReconnectionAttempts = int.MaxValue,
                    Transports = ImmutableList<string>.Empty/*.Add(Polling.NAME)*/.Add(WebSocket.NAME)
                };

                _socket = IO.Socket(Host, options);

                SubscribeOnEvents();
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Bet18 Exception {JsonConvert.SerializeObject(e)}");
            }
        }

        private void GetToken()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers["x-requested-with"] = "XMLHttpRequest";

                    var a = wc.UploadString("https://www.18bet.co/en/auth/operation/getOddsToken", "system_data%5Bcurrent_url%5D=https%3A%2F%2Fwww.18bet.co%2Fen%2Fsport%2Flive&method=getLcToken");

                    _token = JsonConvert.DeserializeObject<dynamic>(a).token;

                    _exp = JsonConvert.DeserializeObject<dynamic>(a).exp;
                }
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Bet18 Exception {JsonConvert.SerializeObject(e)}");
            }
        }

        private void SubscribeOnEvents()
        {
            _socket.On(Socket.EVENT_CONNECT, () =>
            {
                try
                {
                    Log.Info($"Bet18 EVENT_CONNECT");

                    if (_rooms.ContainsKey("updateOdds")) return;

                    //Log.Info($"Bet18 updateOdds");

                    dynamic obj = new JObject();

                    obj.event_state = "live";
                    obj.index = 3;
                    obj.init = 1;
                    obj.is_main_gp = 1;
                    obj.is_main_mt = 1;
                    obj.locale = "en_EN";
                    obj.odds_format = "decimal";
                    obj.type = "odds";

                    _socket.Emit("subscribe", obj, "updateOdds");

                    //_rooms.Add("updateOdds", string.Empty);
                }
                catch (Exception e)
                {
                    Log.Info($"ERROR Bet18 Exception {JsonConvert.SerializeObject(e)}");
                }
            });

            _socket.On("room", o =>
            {
                try
                {
                    //Log.Info($"Bet18 room");

                    var r = o as object[];
                    var roomId = r[0].ToString();
                    var roomName = r[1].ToString();

                    if (_rooms.ContainsKey(roomName))
                        _rooms[roomName] = roomId;
                    else
                        _rooms.Add(roomName, roomId);
                }
                catch (Exception e)
                {
                    Log.Info($"ERROR Bet18 Exception {JsonConvert.SerializeObject(e)}");
                }
            });

            _socket.On("initial", o =>
            {
                //Log.Info($"Bet18 initial");

                lock (_sync) ConvertData(o);

            });

            _socket.On("changes", o =>
            {
                lock (_sync) ConvertData(o);
            });

            _socket.On("initial_chunk", o =>
            {
                //Log.Info($"Bet18 initial_chunk");

                lock (_sync) ConvertData(o);
            });

            _socket.On("initial_chunk_end", (data) =>
            {
                //Log.Info($"Bet18 initial_chunk_end");
            });


            _socket.On(Socket.EVENT_DISCONNECT, (data) =>
            {

                try
                {
                    lock (_sync) _rooms.Clear();
                    lock (_sync) _events.Clear();
                    lock (_sync) _subscribedMarkets.Clear();

                    _socket.Off();
                    _socket.Close();

                    Log.Info($"ERROR Bet18 EVENT_DISCONNECT {JsonConvert.SerializeObject(data)}");
                    InitConnection();
                }
                catch (Exception e)
                {
                    Log.Info($"ERROR Bet18 Exception {JsonConvert.SerializeObject(e)}");
                }


            });

            _socket.On(Socket.EVENT_RECONNECT, (attemptNumber) =>
            {
                Log.Info($"ERROR Bet18 EVENT_RECONNECT {JsonConvert.SerializeObject(attemptNumber)}");
            });

            _socket.On(Socket.EVENT_RECONNECTING, (attemptNumber) =>
            {
                Log.Info($"ERROR Bet18 EVENT_RECONNECTING {JsonConvert.SerializeObject(attemptNumber)}");
            });

            _socket.On(Socket.EVENT_RECONNECT_ATTEMPT, (attemptNumber) =>
            {
                Log.Info($"ERROR Bet18 EVENT_RECONNECT_ATTEMPT {JsonConvert.SerializeObject(attemptNumber)}");
            });

            _socket.On(Socket.EVENT_ERROR, (data) =>
            {
                Log.Info($"ERROR Bet18 EVENT_ERROR {JsonConvert.SerializeObject(data)}");
            });
        }

        private void SubscribeAllMarkets()
        {
            var eventsList = _events.Values.Select(e => e.event_id.ToString()).ToList();

            if (eventsList.All(evId => _subscribedMarkets.Contains(evId))) return;

            if (_rooms.ContainsKey("getEventMarkets"))
            {
                _socket.Emit("unsubscribe", _rooms["getEventMarkets"]);
                _rooms.Remove("getEventMarkets");
            }


            dynamic obj = new JObject();

            obj.event_id = JToken.FromObject(eventsList);
            obj.init = 1;
            obj.locale = "en_EN";
            obj.odds_format = "decimal";
            obj.type = "odds";

            _socket.Emit("subscribe", obj, "getEventMarkets");

            //Log.Info($"Bet18 SubscribeAllMarkets");

            _subscribedMarkets = eventsList;
        }

        private void ConvertData(object data)
        {
            try
            {
                var r = data as object[];
                var e = r[1] as JArray;

                foreach (var obj in e)
                {
                    var type = obj[0].ToObject<string>();

                    //event
                    switch (type)
                    {
                        //new event
                        case "i":
                            {
                                var id = obj[1].ToObject<long>();
                                var ev = obj[2].ToObject<Event>();

                                _events.GetOrAdd(id, ev);

                                break;
                            }
                        //remove event
                        case "r":
                            var eventId = obj[1].ToObject<long>();

                            _events.TryRemove(eventId, out var _);

                            break;
                        case "im":
                            {
                                var id = obj[1].ToObject<long>();
                                var market = obj[2].ToObject<Market>();

                                _events.TryGetValue(market.event_id, out var @event);

                                if (@event == null) continue;

                                if (!@event.markets.ContainsKey(id))
                                    @event.markets.Add(id, market);
                                else
                                    @event.markets[id] = market;
                                break;
                            }
                        case "u":
                            {
                                var evId = obj[1].ToObject<long>();
                                var eventToUpdate = obj[2].ToObject<EventUpdate>();

                                _events.TryGetValue(evId, out var @event);

                                if (@event == null) continue;

                                @event.all_markets = eventToUpdate.all_markets ?? @event.all_markets;
                                @event.live_score_home = eventToUpdate.live_score_home ?? @event.live_score_home;
                                @event.live_score_away = eventToUpdate.live_score_away ?? @event.live_score_away;
                                @event.is_hidden = eventToUpdate.is_hidden ?? @event.is_hidden;
                                @event.event_status = eventToUpdate.event_status != null && eventToUpdate.event_status.IsNotEmpty() ? eventToUpdate.event_status : @event.event_status;
                                break;
                            }
                        case "rm":
                            {
                                var marketId = obj[1].ToObject<long>();
                                var evId = obj[2].ToObject<long>();

                                _events.TryGetValue(evId, out var @event);

                                if (@event?.markets.ContainsKey(marketId) == true)
                                    @event.markets.Remove(marketId);

                                break;
                            }
                        case "um":
                            {
                                var marketId = obj[1].ToObject<long>();
                                var marketToUpdate = obj[2].ToObject<MarketUpdate>();

                                _events.TryGetValue(marketToUpdate.event_id, out var @event);

                                Market market = null;

                                @event?.markets.TryGetValue(marketId, out market);

                                if (market == null) continue;

                                market.is_hidden = marketToUpdate.is_hidden ?? market.is_hidden;
                                market.is_suspended = marketToUpdate.is_suspended ?? market.is_suspended;

                                foreach (var odd in marketToUpdate.odds)
                                {
                                    if (market.odds.ContainsKey(odd.Key))
                                        market.odds[odd.Key] = odd.Value;
                                    else
                                        market.odds.Add(odd.Key, odd.Value);
                                }
                                break;
                            }

                    }
                }
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Bet18 Exception {JsonConvert.SerializeObject(e)}");
            }
        }

        private DateTime? _showLog;

        protected override void UpdateLiveLines()
        {
            try
            {
                var scanner = new Bet18LineConverter();

                string serializedString;

                lock (_sync) serializedString = JsonConvert.SerializeObject(_events.Values);

                var copy = JsonConvert.DeserializeObject<List<Event>>(serializedString);

                var lines = scanner.Convert(copy, Name);

                if (_showLog == null || (DateTime.Now - _showLog).Value.Seconds > 1)
                {
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

}
