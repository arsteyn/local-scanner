using System.Collections.Generic;
using Newtonsoft.Json;

namespace S888.Models.Line
{
    public class HeaderSection
    {
        [JsonProperty(PropertyName = "result_type_id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "result_type_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "market_name")]
        public string BetGroupName { get; set; }

        [JsonProperty(PropertyName = "market_suspend")]
        public string IsDisabled { get; set; }

        [JsonProperty(PropertyName = "outcomes")]
        public List<Odds> Odds { get; set; }

        public Section Section { get; set; } // композиция для расширения функционала, т.е поведения.

        public void InitHeaderSection()
        {
            var bet = new Bet
            {
                IsDisabled = IsDisabled,
                Odds = Odds
            };

            var betGroup = new BetGroup
            {
                Name = BetGroupName, 
                Bets = new List<Bet>()
            };
            betGroup.Bets.Add(bet);

            Section = new Section
            {
                Id = Id,
                Name = Name,
                BetsGroup = new List<BetGroup>()
            };
            Section.BetsGroup.Add(betGroup);
        }
    }
}
