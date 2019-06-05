using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Dafabet.Models
{

    public class BaseDataResult<T>
    {
        public int ErrorCode { get; set; }
        public string ErrorMsg { get; set; }

        public T Data { get; set; }
    }

    internal class Game
    {
        public int GameId { get; set; }
        public string Name { get; set; }

        public M0 M0 { get; set; }

    }

    internal class M0
    {
        public int E { get; set; }
        public int T { get; set; }
        public int L { get; set; }
    }

    public class ShowAllOddData
    {
        public List<NewMatch> NewMatch { get; set; }
        public JObject TeamN { get; set; }
        public JObject LeagueN { get; set; }
    }

    public class NewMatch
    {
        public long MatchId { get; set; }
        public int GameId { get; set; }
        public long LeagueId { get; set; }
        public long TeamId1 { get; set; }
        public long TeamId2 { get; set; }
        public int T1V { get; set; }
        public int T2V { get; set; }
        public bool IsLive { get; set; }
    }

    public class GetMarketData
    {
        public Markets Markets { get; set; }
    }

    public class Markets
    {
        public List<NewOdd> NewOdds { get; set; }
    }

    public class NewOdd
    {
        public long MarketId { get; set; }
        public long MatchId { get; set; }
        public int BetTypeId { get; set; }
        public decimal? Line { get; set; }
        public bool Pim { get; set; }

        public JObject Selections { get; set; }
    }

    public class Selections
    {
        public string SelId { get; set; }
        public decimal Price { get; set; }
        public decimal Seq { get; set; }
    }
}

