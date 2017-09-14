using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bars.EAS.Utils;
using BM;
using BM.Core;
using BM.DTO;
using BM.Interfaces;
using Parimatch.AdditionalClasses;
using Parimatch.Models;

namespace Parimatch
{
    public class ParimatchLineConverter : ILineConverter<TempData>
    {

        private List<LineDTO> _lines;
        private string _bookmakerName;
        private static readonly string[] Types = { "yellow cards", "offsides", "corners" };
        private static readonly Dictionary<string, string> CoefMap = new Dictionary<string, string>(OutcomeHelper.SimpleKoefsMap)
        {
            { "HAND.ODDS1", "HANDICAP1" },
            { "HAND.ODDS2", "HANDICAP2" },
            { "TOTALOVER", "TOTALOVER" },
            { "TOTALUNDER", "TOTALUNDER" },
            { "ITOTALOVER1", "ITOTALOVER1" },
            { "ITOTALOVER2", "ITOTALOVER2" },
            { "ITOTALUNDER1", "ITOTALUNDER1" },
            { "ITOTALUNDER2", "ITOTALUNDER2" },
        };

        readonly Dictionary<string, string> _errors = new Dictionary<string, string>();

        public LineDTO[] Convert(TempData data, string bookmakerName)
        {
            _bookmakerName = bookmakerName;

            _lines = new List<LineDTO>();

            var events = ParserHelper.ParseHtml(data.Html);

            if (events == null)
            {
                return null;
            }

            foreach (var @event in events)
            {
                Convert(@event);
            }

            return _lines.ToArray();
        }

        private void Convert(Event @event)
        {
            var type = Types.FirstOrDefault(x => @event.Home.StartsWith(x) && @event.Away.StartsWith(x));

            if (type != null)
            {
                @event.DefaultType = type;
            }

            foreach (var betBlock in @event.BetBlocks)
            {
                if (betBlock.Name.Contains("Team total"))
                {
                    ConvertTeamTotals(betBlock);
                    continue;
                }

                foreach (var outcome in betBlock.Outcomes)
                {
                    switch (betBlock.Name)
                    {
                        case "Main Bets":
                            ConvertMainBets(outcome);
                            continue;
                        case "Add. totals":
                            ConvertAdditionalTotals(outcome);
                            continue;
                        case "Add. handicaps":
                            ConvertHandicaps(outcome);
                            continue;
                        case "Money line":
                            ConvertMoneyLine(outcome);
                            continue;
                        default:

                            _errors[betBlock.Name] = "betblock name not found";
                            continue;
                    }
                }
            }
        }

        readonly Regex _teamTotalRegex = new Regex("^(?<period>.*?)[.] Team total (?<team>.*?)$", RegexOptions.Compiled);
        private void ConvertTeamTotals(BetBlock betBlock)
        {
            var match = _teamTotalRegex.Match(betBlock.Name);

            if (!match.Success)
            {
                _errors[betBlock.Name] = "team total error";
                return;
            }

            var period = match.Groups["period"].Value;

            if (string.IsNullOrEmpty(period) || !Helper.TypeMap.ContainsKey(period))
            {
                return;
            }

            var coefType = Helper.TypeMap[period];
            var team = match.Groups["team"].Value;

            string coefKind;
            if (team == betBlock.Event.Home)
            {
                coefKind = "ITOTAL{0}1";
            }
            else if (team == betBlock.Event.Away)
            {
                coefKind = "ITOTAL{0}2";
            }
            else
            {
                return;
            }

            foreach (var outcome in betBlock.Outcomes)
            {
                var lineDto = CreateLineDto(outcome, coefType);

                lineDto.CoeffKind = string.Format(coefKind, outcome.Name).ToUpper();

                AddLine(lineDto);
            }
        }

        private void ConvertMoneyLine(Outcome outcome, string coefType = null)
        {
            string coeffKind;

            if (outcome.Name.StartsWith(outcome.Event.Home))
            {
                coeffKind = "W1";
            }
            else if (outcome.Name.StartsWith(outcome.Event.Away))
            {
                coeffKind = "W2";
            }
            else
            {
                return;
            }

            var lineDto = CreateLineDto(outcome, coefType);

            lineDto.CoeffKind = coeffKind;

            AddLine(lineDto);
        }

        private void ConvertAdditionalTotals(Outcome outcome, string coefType = null)
        {
            string coeffKind;
            if (outcome.Name.StartsWith(outcome.Event.Home))
            {
                coeffKind = $"{outcome.Name.Replace(outcome.Event.Home, "ITOTAL")}1";
            }
            else if (outcome.Name.StartsWith(outcome.Event.Away))
            {
                coeffKind = $"{outcome.Name.Replace(outcome.Event.Away, "ITOTAL")}2";
            }
            else
            {
                coeffKind = $"TOTAL{outcome.Name}";
            }

            coeffKind = coeffKind.ToUpper();

            if (!CoefMap.ContainsKey(coeffKind))
            {
                return;
            }

            var lineDto = CreateLineDto(outcome, coefType);

            lineDto.CoeffKind = coeffKind;

            AddLine(lineDto);
        }

        private void ConvertHandicaps(Outcome outcome, string coefType = null)
        {
            string coeffKind;
            var team = outcome.Name;

            if (team == outcome.Event.Home)
            {
                coeffKind = "HANDICAP1";
            }
            else if (team == outcome.Event.Away)
            {
                coeffKind = "HANDICAP2";
            }
            else
            {
                return;
            }

            var lineDto = CreateLineDto(outcome, coefType);
            lineDto.CoeffKind = coeffKind;

            AddLine(lineDto);
        }

        private void ConvertMainBets(Outcome outcome)
        {
            string coefType = null;
            if (!string.IsNullOrEmpty(outcome.BetBlock.PriodName) && !Helper.TypeMap.TryGetValue(outcome.BetBlock.PriodName, out coefType))
            {
                _errors[outcome.BetBlock.PriodName] = "not in TypeMap";
                return;
            }

            string coeffKind;
            var name = outcome.Name.ToUpper();

            if (!CoefMap.TryGetValue(name, out coeffKind))
            {
                _errors[name] = "not in CoefMap";
                return;
            }

            var lineDto = CreateLineDto(outcome, coefType);
            lineDto.CoeffKind = coeffKind;

            AddLine(lineDto);
        }

        private LineDTO CreateLineDto(Outcome outcome, string coefType)
        {
            var @event = outcome.Event;

            if (@event.DefaultType != null)
            {
                coefType = string.IsNullOrEmpty(coefType) ? @event.DefaultType : $"{coefType} {@event.DefaultType}";
            }

            var lineDto = new LineDTO
            {
                BookmakerName = _bookmakerName,
                SportKind = @event.Sport,
                EventDate = @event.Date,
                CoeffType = coefType,
                Team1 = @event.Home,
                Team2 = @event.Away,
                Score1 = @event.Score1.ToInt(),
                Score2 = @event.Score2.ToInt(),
                CoeffParam = outcome.Param,
                CoeffValue = outcome.Value,
                LineObject = outcome.Id,
                ObjectCreateDate = DateTime.Now
            };

            return lineDto;
        }

        private readonly List<string> _stopWords = new List<string> {"shot", "target", "total", "corner", "overall"}; 

        private void AddLine(LineDTO lineDto)
        {

            if (_stopWords.Any(sw=>lineDto.Team1.Contains(sw)) || _stopWords.Any(sw => lineDto.Team2.Contains(sw))) return;

            lineDto.UpdateName();
            _lines.Add(lineDto);
        }
    }
}


