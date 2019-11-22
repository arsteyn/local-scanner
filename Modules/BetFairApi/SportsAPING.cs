
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

        public static readonly string LIST_MARKET_TYPES = BuildUrl("listMarketTypes");
        public static readonly string LIST_MARKET_BOOK = BuildUrl("listMarketBook");


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

            tasks.AddRange(marketIds.Split(40)
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism).Select(marketIdsSplit =>
                Task.Factory.StartNew(
                    state =>
                    {
                        var request = new JsonRequest
                        {
                            Id = 1,
                            Method = LIST_MARKET_BOOK,
                            Params = new { marketIds = marketIdsSplit, priceProjection, orderProjection, matchProjection, currencyCode }
                        };
                        try
                        {
                            var invokeResult = Invoke<IList<MarketBook>>(request);

                            lock (Lock)
                            {
                                result.AddRange(invokeResult);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Console.WriteLine(e.InnerException);
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


        public T Invoke<T>(JsonRequest request)
        {
            using (var wc = this.GetBetWebClient(this.ApiKey, this.Token))
            {
                var response = wc.Post(URL, request);
                var r = JsonConvert.DeserializeObject<JsonResponse<T>>(response);
                return r.Result;
            }
        }

        public string Token { get; set; }
    }
}
