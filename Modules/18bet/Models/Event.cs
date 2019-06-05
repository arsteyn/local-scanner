using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bet18.Models
{
    public class Event
    {
        public Event()
        {
            this.markets = new Dictionary<long, Market>();
        }

        public long event_id { get; set; }
        public string event_state { get; set; }
        public string sport_title { get; set; }
        //1 soccer
        //2 tennis
        //3 basketball
        //4 volleyball
        //10 tabletennis
        //19 badminton
        //?? ice hockey
        public int sport_id { get; set; }
        public string league_id { get; set; }
        public string home_team { get; set; }
        public string away_team { get; set; }
        public string league_title { get; set; }
        public int all_markets { get; set; }
        public int live_score_home { get; set; }
        public int live_score_away { get; set; }
        public bool is_hidden { get; set; }
        
        //running
        public string event_status { get; set; }
        public Dictionary<long, Market> markets { get; set; }
    }

    public class EventUpdate
    {
        public int? all_markets { get; set; }
        public int? live_score_home { get; set; }
        public int? live_score_away { get; set; }
        public bool? is_hidden { get; set; }
        //running
        public string event_status { get; set; }

    }

    public class Market
    {
        public Market()
        {
            this.odds = new Dictionary<string, Odd>();
        }

        public long event_id { get; set; }
        public string market_id { get; set; }
        public string market_key { get; set; }
        public bool is_ft { get; set; }
        public bool is_fh { get; set; }
        public bool is_hidden { get; set; } 
        public bool is_suspended { get; set; }
        public int game_period_id { get; set; }
        public int line_entity_id { get; set; }
        public int market_type_id { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? invalidated_at { get; set; }

        public Dictionary<string, Odd> odds { get; set; }
    }

    public class MarketUpdate
    {
        public MarketUpdate()
        {
            this.odds = new Dictionary<string, Odd>();
        }

        public long event_id { get; set; }
        public bool? is_hidden { get; set; }
        public bool? is_suspended { get; set; }
        public Dictionary<string, Odd> odds { get; set; }
    }

    public class Odd
    {
        [JsonProperty(PropertyName = "as")]
        public string _as { get; set; }
        public decimal? es { get; set; }
        public decimal? o { get; set; }
        public string k { get; set; }
        public string v { get; set; }
        public string id { get; set; }
    }


}



