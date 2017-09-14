using System;
using System.Collections.Generic;
using System.Linq;
using BM.DTO;
using WilliamHill.Models;
using WilliamHill.SerializableClasses;

namespace WilliamHill
{
    public static class ConverterHelper
    {
        public static List<LineDTO> CreateWinLines(Market market, string coeffType = null)
        {
            var map = new Dictionary<string, string>
            {
                [market.Event.Home] = "1",
                ["Draw"] = "X",
                ["Tie"] = "X",
                [market.Event.Away] = "2"
            };


            return CreateLines(market, map, coeffType);
        }

        public static List<LineDTO> CreateHandicapLines(Market market, string coeffType = null)
        {
            coeffType = coeffType?.Replace("Alternative", "");

            var map = new Dictionary<string, string>
            {
                [market.Event.Home] = "HANDICAP1",
                [market.Event.Away] = "HANDICAP2"
            };

            return CreateLines(market, map, coeffType);
        }

        public static List<LineDTO> CreateTotalLines(Market market, string coeffType = null)
        {
            var map = new Dictionary<string, string>
            {
                ["Under"] = "TOTALUNDER",
                ["Over"] = "TOTALOVER"
            };

            if (coeffType.Contains(market.Event.Home))
            {
                map["Under"] = "ITOTALUNDER1";
                map["Over"] = "ITOTALOVER1";
                coeffType = coeffType.Replace(market.Event.Home, "").Trim();
            }
            else if (coeffType.Contains(market.Event.Away))
            {
                map["Under"] = "ITOTALUNDER2";
                map["Over"] = "ITOTALOVER2";
                coeffType = coeffType.Replace(market.Event.Away, "").Trim();
            }

            return CreateLines(market, map, coeffType);
        }

        public static List<LineDTO> CreateLines(Market market, Dictionary<string, string> map, string coeffType = null)
        {
            var lines = new List<LineDTO>();

            if (coeffType == "Match")
            {
                coeffType = null;
            }

            foreach (var selection in market.Selections.Where(x => map.ContainsKey(x.Name)))
            {
                var line = new LineDTO
                {
                    Team1 = market.Event.Home,
                    Team2 = market.Event.Away,
                    Score1 = market.Event.Score1,
                    Score2 = market.Event.Score2,
                    Pscore1 = market.Event.Pscore1,
                    Pscore2 = market.Event.Pscore2,
                    EventDate = market.Event.EventDate.AddHours(3),
                    CoeffValue = selection.Price.CoeffValue,
                    CoeffKind = map[selection.Name],
                    CoeffType = coeffType,
                    Url = $"http://sports.whgaming.com/bet/ru/betting/e/{market.Event.Id}"
                };

                if (selection.HcapInfo != null)
                {
                    line.CoeffParam = selection.HcapInfo.HcapString.ToNullDecimal();
                }

                line.SerializeObject(CreateSlipData(selection));
                
                lines.Add(line);
            }

            return lines;
        }

        static SlipData CreateSlipData(Selection selection)
        {
            var slipData = new SlipData
            {
                ev_oc_id = selection.Id.ToString(),
                lp_num = selection.Price.Num.ToString(),
                lp_den = selection.Price.Den.ToString(),
                hcap_value = selection.HcapInfo == null ? "" : selection.HcapInfo.HcapString
            };

            return slipData;
        }

        public static IEnumerable<LineDTO> CreateDnbLines(Market market, string coeffType)
        {
            var map = new Dictionary<string, string>();
            map[market.Event.Home] = "W1";
            map[market.Event.Away] = "W2";

            return CreateLines(market, map, coeffType);
        }
    }
}
