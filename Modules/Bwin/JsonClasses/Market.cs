using System.Collections.Generic;

namespace Bwin.JsonClasses
{
    public class Market
    {
        public List<Result> Results { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }
        public string Self { get; set; }
        public bool Visible { get; set; }
        public string GameTemplateId { get; set; }
        public bool IsMainBook { get; set; }
        public string MarketOrder { get; set; }
        public bool IsExt { get; set; }
        public bool IsMain { get; set; }

    }

    public class Result
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Self { get; set; }
        public bool Visible { get; set; }
        public decimal Odds { get; set; }
        public string GameTemplateId { get; set; }
    }
}