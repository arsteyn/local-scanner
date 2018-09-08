
using System.Diagnostics;
using System.Threading.Tasks;

namespace BetFairApi
{
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using Web;

    public class SportsAPING : APING
    {
        public SportsAPING(string appKey, string token)
            : base(appKey)
        {
            this.Token = token;
        }

        public const string SERVICE = "SportsAPING";

        public const string VERSION = "v1.1";

        public const string URL = "https://api.betfair.com/exchange/betting/json-rpc/v1";

        static readonly object Lock = new object();

        public static string BuildUrl(string method)
        {
            return $"{SERVICE}/{VERSION}/{method}";
        }

        public static readonly string LIST_EVENT_TYPES = BuildUrl("listEventTypes");
        public static readonly string LIST_EVENTS = BuildUrl("listEvents");
        public static readonly string LIST_MARKET_TYPES = BuildUrl("listMarketTypes");
        public static readonly string LIST_MARKET_BOOK = BuildUrl("listMarketBook");
        public static readonly string PLACE_ORDERS = BuildUrl("placeOrders");

        public static readonly string LIST_CURRENT_ORDERS = BuildUrl("listCurrentOrders");
        public static readonly string CANCEL_ORDERS = BuildUrl("cancelOrders");



        /// <summary>
        /// Виды спорта
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IList<EventTypeResult> ListEventTypes(MarketFilter filter)
        {
            filter.IfNull(x => { throw new ArgumentException("filter"); });

            var param = new JsonRequest
            {
                Id = 1,
                Method = LIST_EVENT_TYPES,
                Params = new { filter }
            };

            return Invoke<IList<EventTypeResult>>(param);
        }

        /// <summary>
        /// События
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IList<EventResult> ListEvents(MarketFilter filter)
        {
            filter.IfNull(x => { throw new ArgumentException("filter"); });

            var request = new JsonRequest
            {
                Id = 1,
                Method = LIST_EVENTS,
                Params = new { filter }
            };

            return Invoke<IList<EventResult>>(request);
        }

        /// <summary>
        /// Виды ставок
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="maxResults"></param>
        /// <returns></returns>
        public IList<MarketCatalogue> ListMarketCatalogue(MarketFilter filter, int maxResults)
        {
            return ListMarketCatalogue(filter, null, null, maxResults);
        }

        /// <summary>
        /// Виды ставок
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="marketProjection"></param>
        /// <param name="sort"></param>
        /// <param name="maxResults"></param>
        /// <returns></returns>
        public IList<MarketCatalogue> ListMarketCatalogue(MarketFilter filter, IList<MarketProjection> marketProjection, MarketSort? sort, int maxResults)
        {
            filter.IfNull(x => throw new ArgumentException("filter"));

            var request = new JsonRequest
            {
                Id = 1,
                Method = $"{SERVICE}/{VERSION}/listMarketCatalogue",
                Params = new { filter, marketProjection, sort, maxResults }
            };

            return Invoke<IList<MarketCatalogue>>(request);
        }

        public IList<MarketTypeResult> ListMarketTypes(MarketFilter filter)
        {
            filter.IfNull(x => { throw new ArgumentException("filter"); });

            var request = new JsonRequest
            {
                Id = 1,
                Method = LIST_MARKET_TYPES,
                Params = new { filter }
            };

            return Invoke<IList<MarketTypeResult>>(request);
        }

        public IList<MarketBook> ListMarketBook(List<string> marketIds, PriceProjection priceProjection, OrderProjection? orderProjection, MatchProjection? matchProjection, string currencyCode = null)
        {
            marketIds.IfNull(x => { throw new ArgumentException("marketIds"); });

            var result = new List<MarketBook>();

            var st = new Stopwatch();

            st.Start();

            var tasks = new List<Task>();

            tasks.AddRange(marketIds.Split(40).AsParallel().WithDegreeOfParallelism(4).Select(marketIdsSplit =>
                Task.Factory.StartNew(
                    state =>
                    {
                        var request = new JsonRequest
                        {
                            Id = 1,
                            Method = LIST_MARKET_BOOK,
                            Params = new { marketIds = marketIdsSplit, priceProjection, orderProjection, matchProjection, currencyCode }
                        };


                        lock (Lock)
                        {
                            result.AddRange(Invoke<IList<MarketBook>>(request));
                        }

                    }, marketIdsSplit)));

            try
            {
                Task.WaitAll(tasks.ToArray(), 10000);
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.InnerException);
            }

            st.Stop();

            return result;
        }

        public PlaceExecutionReport PlaceOrders(string marketId, List<PlaceInstruction> instructions, string customerRef = null)
        {
            marketId.IfNull(x => { throw new ArgumentException("marketId"); });
            instructions.IfNull(x => { throw new ArgumentException("instructions"); });

            var request = new JsonRequest
            {
                Id = 1,
                Method = PLACE_ORDERS,
                Params = new { marketId, instructions, customerRef }
            };

            return Invoke<PlaceExecutionReport>(request);
        }

        public CurrentOrderSummaryReport ListCurrentOrders()
        {
            var request = new JsonRequest
            {
                Id = 1,
                Method = LIST_CURRENT_ORDERS,
                Params = new
                {
                    orderBy = OrderBy.BY_MATCH_TIME,
                    orderProjection = OrderProjection.EXECUTION_COMPLETE
                }
            };

            return Invoke<CurrentOrderSummaryReport>(request);
        }


        public CancelExecutionReport CancelOrder(string marketId, string betid, double? reduceSize = null)
        {
            var request = new JsonRequest
            {
                Id = 1,
                Method = CANCEL_ORDERS,
                Params = new
                {
                    marketId,
                    instructions = new List<CancelInstruction>
                    {
                        new CancelInstruction {
                            BetId = betid,
                            SizeReduction = reduceSize
                        }
                    }
                }
            };

            return Invoke<CancelExecutionReport>(request);
        }

        public T Invoke<T>(JsonRequest request)
        {
            using (var wc = this.GetBetWebClient(this.ApiKey, this.Token))
            {
                var response = wc.Post<JsonResponse<T>>(URL, request);
                return response.Result;
            }
        }

        public string Token { get; set; }
    }
}
