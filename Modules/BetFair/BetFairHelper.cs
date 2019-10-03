using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bars.EAS;
using Bars.EAS.Utils.Extension;
using BetFair.Enums;
using BetFairApi;
using BM;
using BM.Core;
using BM.DTO;
using BM.Entities;
using BM.Web;
using BM.Web.Interfaces;
using Newtonsoft.Json;
using NLog;
using NLog.Fluent;

namespace BetFair
{
    public static class BetFairHelper
    {
        static Logger Log => LogManager.GetCurrentClassLogger();

        public static string GetAuthField(CookieCollection collection, AuthField authField)
        {
            return collection.GetValue(authField.ToString());
        }

        public static IList<LineDTO> Convert(MarketCatalogue marketCatalogue, MarketBook marketBook, Action<LineDTO> action)
        {
            var lines = new List<LineDTO>();

            foreach (var runner in marketBook.Runners)
            {
                var coeffKind = GetCoeffKind(new GetCoeffKindParams(runner, marketCatalogue, runner.Handicap), out var сoeffParam);

                if (string.IsNullOrEmpty(coeffKind)) continue;

                var coeffType = GetCoeffType(new GetCoeffKindParams(runner, marketCatalogue, runner.Handicap));

                //берем меньший кэф
                var price = runner.ExchangePrices.AvailableToBack.OrderBy(x => x.Price).FirstOrDefault(x => x.Size > 200.0);

                if (price == null) continue;

                var line = new LineDTO
                {
                    CoeffParam = сoeffParam,
                    CoeffKind = coeffKind,
                    CoeffValue = (decimal)price.Price,
                    CoeffType = coeffType
                };

                line.SerializeObject(new LineInfo
                {
                    Size = price.Size,
                    MarketId = marketBook.MarketId,
                    SelectionId = runner.SelectionId,
                    Handicap = runner.Handicap
                });

                //line.LineData = string.Join(",", runner.ExchangePrices.AvailableToLay.Where(x => x.Size > 10.0).Select(f => (decimal)f.Price));

                action(line);
                line.UpdateName();
                lines.Add(line);
            }

            return lines;
        }
        private static string GetCoeffType(GetCoeffKindParams getCoeffKindParams)
        {
            var result = string.Empty;

            if (getCoeffKindParams.MarketCatalogue.MarketName.ContainsIgnoreCase("first half"))
            {
                result += "1st half";
            }

            return result;
        }

        private static string GetCoeffKind(GetCoeffKindParams getCoeffKindParams, out decimal? сoeffParam)
        {
            сoeffParam = null;
            var marketCatalogue = getCoeffKindParams.MarketCatalogue;

            var runnerDescription = marketCatalogue.Runners.FirstOrDefault(x => x.SelectionId == getCoeffKindParams.Runner.SelectionId && x.Handicap == getCoeffKindParams.Runner.Handicap);

            if (runnerDescription == null)
                return null;

            try
            {
                if (marketCatalogue.MarketName.EqualsIgnoreCase("draw no bet"))
                {
                    return getCoeffKindParams.Mapping.ContainsKey(runnerDescription.RunnerName) ?
                        "W" + getCoeffKindParams.Mapping[runnerDescription.RunnerName] : null;
                }
                if (marketCatalogue.MarketName.EqualsIgnoreCase("Match Odds")
                    || marketCatalogue.MarketName.EqualsIgnoreCase("Moneyline")
                    || marketCatalogue.MarketName.EqualsIgnoreCase("Double Chance")
                    || marketCatalogue.MarketName.EqualsIgnoreCase("Regular Time Match Odds"))
                {
                    return getCoeffKindParams.Mapping.ContainsKey(runnerDescription.RunnerName) ?
                        getCoeffKindParams.Mapping[runnerDescription.RunnerName] : null;
                }

                if (marketCatalogue.MarketName.EqualsIgnoreCase("Goal Lines"))
                {
                    сoeffParam = runnerDescription.Handicap.ToNullDecimal();
                    return $"TOTAL{runnerDescription.RunnerName.ToUpper()}";
                }

                if (marketCatalogue.MarketName.EqualsIgnoreCase("asian handicap"))
                {
                    сoeffParam = runnerDescription.Handicap.ToNullDecimal();

                    return getCoeffKindParams.Mapping.ContainsKey(runnerDescription.RunnerName) ? $"HANDICAP{getCoeffKindParams.Mapping[runnerDescription.RunnerName]}" : null;

                }
                if (marketCatalogue.MarketName.StartsWithIgnoreCase("over") || marketCatalogue.MarketName.StartsWithIgnoreCase("under") || marketCatalogue.MarketName.StartsWithIgnoreCase("First Half Goals"))
                {
                    var match = Regex.Match(runnerDescription.RunnerName, @"(?<type>over|under) (?<value>[+|-]?[\d.]+)\s?");

                    if (match.Success)
                    {
                        сoeffParam = match.Groups["value"].Value.ToNullDecimal().Value;
                        return $"TOTAL{match.Groups["type"].Value.ToUpper()}";
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Info("BF Parse CoeffKindException " + JsonConvert.SerializeObject(e));
            }

            return null;/* runnerDescription.RunnerName + marketCatalogue.MarketName;*/
        }

        private static readonly object Lock = new object();
        public static List<LineDTO> Convert(List<MarketCatalogue> list, SportsAPING aping, string bookmaker, ConcurrentDictionary<string, ScoreResult> scoreResults)
        {
            var lines = new List<LineDTO>();

            var marketIds = list.Select(x => x.MarketId);

            var priceProjection = new PriceProjection { PriceData = new List<PriceData> { PriceData.EX_BEST_OFFERS }, ExBestOffersOverrides = new ExBestOffersOverrides { BestPricesDepth = 5 } };

            var marketBooks = aping.ListMarketBook(marketIds.ToList(), priceProjection, null, MatchProjection.ROLLED_UP_BY_PRICE, "USD");

            var openMarkets = marketBooks.Where(x => x.Status == MarketStatus.OPEN);

            Parallel.ForEach(openMarkets, marketBook =>
            {
                var market = list.FirstOrDefault(x => x.MarketId == marketBook.MarketId);

                if (market == null) return;

                if (!scoreResults.ContainsKey(market.Event.Id)) scoreResults.GetOrAdd(market.Event.Id, GetScoreResult(market.Event.Id, aping.Proxy));

                if (scoreResults[market.Event.Id] == null || scoreResults[market.Event.Id].score == null) return;

                var l = Convert(market, marketBook, x =>
                {
                    x.BookmakerName = bookmaker;
                    x.SportKind = Helper.ConvertSport(market.EventType.Name);

                    var teams = market.Event.Name
                        .Replace(" v ", "|")
                        .Replace(" @ ", "|")
                        .Split('|');

                    x.Team1 = teams.First();
                    x.Team2 = teams.Last();

                    x.Score1 = scoreResults[market.Event.Id].score.home.score;
                    x.Score2 = scoreResults[market.Event.Id].score.away.score;

                    x.EventDate = market.MarketStartTime;
                });

                lock (Lock) lines.AddRange(l);
            });

            return lines;
        }

        public static ScoreResult GetScoreResult(string eventId, WebProxy proxy)
        {
            try
            {
                ScoreResult scoreresult;

                using (var wc = new GetWebClient(proxy))
                    scoreresult = wc.DownloadResult<ScoreResult>($"https://ips.betfair.com/inplayservice/v1/eventTimeline?alt=json&eventId={eventId}&locale=en_GB&productType=EXCHANGE&regionCode=UK");

                return scoreresult;
            }
            catch
            {
                return null;
            }
        }
    }

    public class ScoreResult
    {
        public ScoreContainer score { get; set; }
    }

    public class ScoreContainer
    {
        public Score home { get; set; }
        public Score away { get; set; }
    }

    public class Score
    {
        public int score { get; set; }
    }
}
