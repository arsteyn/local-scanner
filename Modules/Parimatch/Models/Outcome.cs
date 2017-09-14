namespace Parimatch.Models
{
    public class Outcome
    {
        public string Name { get; private set; }
        public decimal Value { get; set; }
        public decimal? Param { get; set; }
        public string Id { get; internal set; }

        public BetBlock BetBlock { get; set; }
        public Event Event { get; set; }

        public void SetName(string name)
        {
            Name = name.Trim();
        }
    }
}