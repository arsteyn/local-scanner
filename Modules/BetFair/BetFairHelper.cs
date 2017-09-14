using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using BetFair.Enums;
using BetFairApi;
using BM;
using BM.Core;
using BM.DTO;
using BM.Entities;
using BM.Web.Interfaces;
using NLog;
using NLog.Fluent;

namespace BetFair
{
    public static class BetFairHelper
    {
        public static AccountAPING GetAccountAPING(BmUser bmUser, ICookieProvider provider)
        {
            return GetAccountAPING(provider.GetCookies(bmUser.Id));
        }

        public static AccountAPING GetAccountAPING(CookieCollection collection)
        {
            var apiKey = GetAuthField(collection, AuthField.ApiKey);

            return apiKey == null ? null : new AccountAPING(apiKey);
        }

        public static SportsAPING GetSportsAPING(BmUser bmUser, ICookieProvider provider, bool buying = false)
        {
            return GetSportsAPING(provider.GetCookies(bmUser.Id), buying);
        }

        public static SportsAPING GetSportsAPING(CookieCollection collection, bool buying = false)
        {
            var appKey = GetAuthField(collection, buying ? AuthField.Customer : AuthField.Developer);
            var token = GetAuthField(collection, AuthField.Token);

            return new SportsAPING(appKey, token);
        }

        public static string GetAuthField(CookieCollection collection, AuthField authField)
        {
            return collection.GetValue(authField.ToString());
        }

        internal static bool FindMarket(SportsAPING aping, LineInfo lineInfo, LineDTO line, out string message)
        {
            var priceProjection = new PriceProjection();
            priceProjection.PriceData = new List<PriceData> { PriceData.EX_ALL_OFFERS, PriceData.EX_BEST_OFFERS };

            var markets = aping.ListMarketBook(new List<string> { lineInfo.MarketId }, priceProjection, null, null, "USD");

            var market = markets.FirstOrDefault();

            if (market == null)
            {
                message = "Не найдена ставка. market == null";
                return false;
            }

            if (market.Status != MarketStatus.OPEN)
            {
                message = $"Рынок закрыт. Статус {market.Status}";
                return false;
            }

            var runner = market.Runners.FirstOrDefault(x => x.SelectionId == lineInfo.SelectionId);

            if (runner == null)
            {
                message = "Не найдена ставка. runner == null";
                return false;
            }

            foreach (var priceSize in runner.ExchangePrices.AvailableToBack)
            {
                if ((decimal)priceSize.Price > line.CoeffValue && (decimal)priceSize.Size > line.Price)
                {
                    line.CoeffValue = (decimal)priceSize.Price;
                    message = $"Price = {priceSize.Price}. Size = {priceSize.Size}";
                    return true;
                }
            }

            message = "";
            return false;
        }

        public static IList<LineDTO> Convert(MarketCatalogue marketCatalogue, MarketBook marketBook, Action<LineDTO> action)
        {
            var lines = new List<LineDTO>();

            foreach (var runner in marketBook.Runners)
            {
                decimal? сoeffParam;
                var coeffKind = GetCoeffKind(new GetCoeffKindParams(runner, marketCatalogue), out сoeffParam);

                if (string.IsNullOrEmpty(coeffKind))
                {
                    continue;
                }

                //берем меньший кэф
                foreach (var price in runner.ExchangePrices.AvailableToBack.Where(x => x.Size > 50.0).Skip(1).Take(1))
                {
                    var line = new LineDTO
                    {
                        CoeffParam = сoeffParam,
                        CoeffKind = coeffKind
                    };

                    line.CoeffValue = (decimal)price.Price;

                    line.SerializeObject(new LineInfo
                    {
                        Size = price.Size,
                        MarketId = marketBook.MarketId,
                        SelectionId = runner.SelectionId
                    });

                    action(line);
                    line.UpdateName();
                    lines.Add(line);
                }
            }

            return lines;
        }

        private static string GetCoeffKind(GetCoeffKindParams getCoeffKindParams, out decimal? сoeffParam)
        {
            сoeffParam = null;
            var marketCatalogue = getCoeffKindParams.MarketCatalogue;

            var runnerDescription = marketCatalogue.Runners
                .FirstOrDefault(x => x.SelectionId == getCoeffKindParams.Runner.SelectionId);

            if (marketCatalogue.MarketName == "Draw No Bet")
            {
                return getCoeffKindParams.Mapping.ContainsKey(runnerDescription.RunnerName) ?
                    "W" + getCoeffKindParams.Mapping[runnerDescription.RunnerName] : null;
            }
            else if (marketCatalogue.MarketName == "Match Odds"
                || marketCatalogue.MarketName == "Moneyline"
                || marketCatalogue.MarketName == "Double Chance"
                || marketCatalogue.MarketName == "Regular Time Match Odds")
            {
                return getCoeffKindParams.Mapping.ContainsKey(runnerDescription.RunnerName) ?
                    getCoeffKindParams.Mapping[runnerDescription.RunnerName] : null;
            }
            else if (marketCatalogue.MarketName.StartsWith(getCoeffKindParams.FirstTeam)
                || marketCatalogue.MarketName.StartsWith(getCoeffKindParams.SecondTeam))
            {
                if (marketCatalogue.Runners.Count > 2)
                {
                    return null;
                }

                var match = Regex.Match(runnerDescription.RunnerName, @"(?<team>.*?) (?<value>[+|-][\d.]+)\s?");

                if (match.Success && getCoeffKindParams.Mapping.ContainsKey(match.Groups["team"].Value))
                {
                    сoeffParam = match.Groups["value"].Value.ToNullDecimal().Value;
                    return $"HANDICAP{getCoeffKindParams.Mapping[match.Groups["team"].Value]}";
                }
            }
            else if (marketCatalogue.MarketName.StartsWith("Over/Under"))
            {
                var match = Regex.Match(runnerDescription.RunnerName, @"(?<type>Over|Under) (?<value>[+|-]?[\d.]+)\s?");

                if (match.Success)
                {
                    сoeffParam = match.Groups["value"].Value.ToNullDecimal().Value;
                    return $"TOTAL{match.Groups["type"].Value.ToUpper()}";
                }
            }

            return runnerDescription.RunnerName + marketCatalogue.MarketName;
        }

        public static List<LineDTO> Convert(List<MarketCatalogue> list, SportsAPING aping, string bookmaker)
        {
            var lines = new List<LineDTO>();

            var marketIds = list.Select(x => x.MarketId);

            var priceProjection = new PriceProjection { PriceData = new List<PriceData> { PriceData.EX_BEST_OFFERS } };

            var marketBooks = aping.ListMarketBook(marketIds.ToList(), priceProjection, null, MatchProjection.ROLLED_UP_BY_PRICE, "USD");

            foreach (var marketBook in marketBooks.Where(x => x.Status == MarketStatus.OPEN))
            {
                var market = list.FirstOrDefault(x => x.MarketId == marketBook.MarketId);

                lines.AddRange(Convert(market, marketBook, x =>
                {
                    x.BookmakerName = bookmaker;
                    x.SportKind = Helper.ConvertSport(market.EventType.Name);

                    var teams = market.Event.Name
                        .Replace(" v ", "|")
                        .Replace(" @ ", "|")
                        .Split('|');

                    x.Team1 = teams.First();
                    x.Team2 = teams.Last();

                    x.EventDate = market.MarketStartTime;
                }));
            }

            return lines;
        }
    }
}
