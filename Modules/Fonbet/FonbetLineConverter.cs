using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using FonBet.SerializedClasses;
using Jil;
using Newtonsoft.Json;

namespace Fonbet
{
    public class FonbetLineConverter
    {
        private List<LineDTO> _lines;

        public List<LineDTO> Convert(LiveData data, string bookmakerName)
        {
            _lines = new List<LineDTO>();

            var eventsMap = data.events.ToDictionary(x => x.id, x => x);
            var sportsMap = data.sports.ToDictionary(x => x.id, x => x);
            var eventMiscsMap = data.eventMiscs.ToDictionary(x => x.id, x => x);
            var blocksMap = data.eventBlocks.ToDictionary(x => x.eventId, x => x);
            var factorsByEvent = data.customFactors.GroupBy(x => x.EventId);

            Parallel.ForEach(factorsByEvent, eventData =>
            {
                var eventId = eventData.Key;

                if (blocksMap.TryGetValue(eventId, out var eventBlock) && eventBlock.state == "blocked")
                    return;

                var @event = eventsMap[eventId];
                var eventMiscs = eventMiscsMap[eventId];
                var sport = sportsMap[@event.SportId];

                while (sport.ParentId.HasValue)
                {
                    sport = sportsMap[sport.ParentId.Value];
                }

                var mainEvent = @event;
                while (mainEvent.ParentId.HasValue)
                {
                    mainEvent = eventsMap[mainEvent.ParentId.Value];
                }

                var mainMisc = eventMiscsMap.ContainsKey(mainEvent.id) ? eventMiscsMap[mainEvent.id] : eventMiscs;

                var type = @event.ParentId.HasValue ? @event.name : string.Empty;
                var startTime = Converter.UnixTimeStampToDateTime(mainEvent.startTime);

                var score = $"{eventMiscs.Score1}:{eventMiscs.Score2}";

                var factors = eventData.Where(x => factorMap.ContainsKey(x.FactorId));

                if (eventBlock != null && eventBlock.state == "partial")
                {
                    factors = factors.Where(x => !eventBlock.factors.Contains(x.FactorId));
                }

                Parallel.ForEach(factors, factor =>
                {
                    var line = new LineDTO
                    {
                        SportKind = Helper.ConvertSport(sport.name),
                        CoeffKind = factorMap[factor.FactorId],
                        Team1 = mainEvent.team1,
                        Team2 = mainEvent.team2,
                        CoeffType = type,
                        EventDate = startTime,
                        CoeffValue = factor.CoeffValue,
                        Score1 = mainMisc.Score1,
                        Score2 = mainMisc.Score2,
                        CoeffParam = factor.CoeffParam.ToNullDecimal(),
                        BookmakerName = bookmakerName,
                        Url = "https://live.fonbet.com/?locale=en#{0}".FormatUsing(eventId),
                        LineData = factor.CoeffParam == null
                            ? new Bet(factor) { Score = score }
                            : new ParamBet(factor) { Score = score }
                    };

                    if (@event.ParentId.HasValue)
                    {
                        line.Pscore1 = eventMiscs.Score1;
                        line.Pscore2 = eventMiscs.Score2;
                    }

                    AddLine(line);
                });
            }
            );

            return _lines;
        }

        private readonly Dictionary<int, string> factorMap = new Dictionary<int, string>
            {
                { 921,  "1"},
                { 922,  "X"},
                { 923,  "2"},
                { 924,  "1X"},
                { 925,  "X2"},
                { 1571, "12"},

                { 910,  "HANDICAP1" },
                { 927,  "HANDICAP1" },
                { 937,  "HANDICAP1" },
                { 989,  "HANDICAP1" },
                { 1845, "HANDICAP1" },
                { 1851, "HANDICAP1" },
                { 1569, "HANDICAP1" },
                { 1672, "HANDICAP1" },
                { 1677, "HANDICAP1" },
                { 1680, "HANDICAP1" },
                { 1683, "HANDICAP1" },
                { 1686, "HANDICAP1" },
                { 1689, "HANDICAP1" },
                { 1692, "HANDICAP1" },
                { 1723, "HANDICAP1" },

                { 912,  "HANDICAP2" },
                { 928,  "HANDICAP2" },
                { 938,  "HANDICAP2" },
                { 991,  "HANDICAP2" },
                { 1846, "HANDICAP2" },
                { 1852, "HANDICAP2" },
                { 1572, "HANDICAP2" },
                { 1675, "HANDICAP2" },
                { 1678, "HANDICAP2" },
                { 1681, "HANDICAP2" },
                { 1684, "HANDICAP2" },
                { 1687, "HANDICAP2" },
                { 1690, "HANDICAP2" },
                { 1718, "HANDICAP2" },
                { 1724, "HANDICAP2" },

                { 930,  "TOTALOVER" },
                { 940,  "TOTALOVER" },
                { 1848, "TOTALOVER" },
                { 1696, "TOTALOVER" },
                { 1727, "TOTALOVER" },
                { 1730, "TOTALOVER" },
                { 1733, "TOTALOVER" },
                { 1736, "TOTALOVER" },
                { 1739, "TOTALOVER" },
                { 1793, "TOTALOVER" },
                { 1796, "TOTALOVER" },
                { 1799, "TOTALOVER" },
                { 1802, "TOTALOVER" },
                { 1805, "TOTALOVER" },

                { 931,  "TOTALUNDER" },
                { 941,  "TOTALUNDER" },
                { 1849, "TOTALUNDER" },
                { 1697, "TOTALUNDER" },
                { 1728, "TOTALUNDER" },
                { 1731, "TOTALUNDER" },
                { 1734, "TOTALUNDER" },
                { 1737, "TOTALUNDER" },
                { 1791, "TOTALUNDER" },
                { 1794, "TOTALUNDER" },
                { 1797, "TOTALUNDER" },
                { 1800, "TOTALUNDER" },
                { 1803, "TOTALUNDER" },
                { 1806, "TOTALUNDER" },

                { 974,  "ITOTALOVER1" },
                { 1809, "ITOTALOVER1" },
                { 1812, "ITOTALOVER1" },
                { 1815, "ITOTALOVER1" },
                { 1818, "ITOTALOVER1" },
                { 1821, "ITOTALOVER1" },
                { 1824, "ITOTALOVER1" },
                { 1827, "ITOTALOVER1" },
                { 1830, "ITOTALOVER1" },
                { 2203, "ITOTALOVER1" },

                { 976,  "ITOTALUNDER1" },
                { 1810, "ITOTALUNDER1" },
                { 1813, "ITOTALUNDER1" },
                { 1816, "ITOTALUNDER1" },
                { 1819, "ITOTALUNDER1" },
                { 1822, "ITOTALUNDER1" },
                { 1825, "ITOTALUNDER1" },
                { 1828, "ITOTALUNDER1" },
                { 1831, "ITOTALUNDER1" },
                { 2204, "ITOTALUNDER1" },

                { 978,  "ITOTALOVER2" },
                { 1854, "ITOTALOVER2" },
                { 1873, "ITOTALOVER2" },
                { 1880, "ITOTALOVER2" },
                { 1883, "ITOTALOVER2" },
                { 1886, "ITOTALOVER2" },
                { 1893, "ITOTALOVER2" },
                { 1896, "ITOTALOVER2" },
                { 1899, "ITOTALOVER2" },
                { 2209, "ITOTALOVER2" },

                { 980,  "ITOTALUNDER2" },
                { 1871, "ITOTALUNDER2" },
                { 1874, "ITOTALUNDER2" },
                { 1881, "ITOTALUNDER2" },
                { 1884, "ITOTALUNDER2" },
                { 1887, "ITOTALUNDER2" },
                { 1894, "ITOTALUNDER2" },
                { 1897, "ITOTALUNDER2" },
                { 1900, "ITOTALUNDER2" },
                { 2210, "ITOTALUNDER2" },

                { 698, "EVEN" },
                { 699, "ODD" },
            };

        readonly object _lock = new object();

    
        private void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            lock (_lock) _lines.Add(lineDto);
        }


    }
}





