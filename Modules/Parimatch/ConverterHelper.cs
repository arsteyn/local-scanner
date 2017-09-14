using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using Bars.EAS.Utils;
using Parimatch.Models;

namespace Parimatch
{
    public static class ParserHelper
    {
        private static readonly HtmlParser HtmlParser = new HtmlParser();
        private static readonly Regex ScoreRegex = new Regex(@"^(?<score1>\d+)-(?<score2>\d+)(?:[(](?<pscores>[\d-,]+)[)])?(?:\s?(?<ss1>\d+):(?<ss2>\d+))?", RegexOptions.Compiled);

        public static List<Event> ParseHtml(string response)
        {
            var sw = Stopwatch.StartNew();

            var document = HtmlParser.Parse(response);

            sw.Stop();

            sw.Restart();

            var rows = document.QuerySelectorAll("form#f1 > div[class='container gray']");

            var events = new List<Event>();

            foreach (var row in rows)
            {
                var sport = GetSport(row);

                var eventHeaderNames = row.QuerySelectorAll("div.wrapper > table > tbody:first-of-type > tr > th").Select(x => x.Text()).ToArray();
                var eventElements = row.QuerySelectorAll("div.wrapper > table > tbody[class^='row']:not(tbody[class~='props'])");

                foreach (var element in eventElements)
                {
                    if (element.TextContent.Contains("Bets are temporary not accepted"))
                    {
                        continue;
                    }

                    var @event = CreateEvent(element);

                    if (@event == null)
                    {
                        continue;
                    }

                    @event.Sport = sport;

                    EventBlocksParserHelper.LoadBlocksByHeader(@event, element, eventHeaderNames);
                    @event.BetBlocks.ForEach(x => x.PriodName = null);

                    var propsElement = element.NextElementSibling;
                    var propsClassName = $"{element.ClassName} props";

                    if (propsElement != null && propsClassName == propsElement.ClassName)
                    {
                        EventBlocksParserHelper.LoadBlocksByHeader(@event, propsElement, eventHeaderNames);
                        EventBlocksParserHelper.LoadPsBlocks(@event, propsElement);
                        EventBlocksParserHelper.LoadP2rBlocks(@event, propsElement);
                        EventBlocksParserHelper.LoadDinBlocks(@event, propsElement);
                    }

                    if (@event.BetBlocks.Any())
                    {
                        events.Add(@event);
                    }
                }
            }

            sw.Stop();

            return events;
        }

        private static string GetSport(IElement element)
        {
            var text = element.QuerySelector(":nth-child(2)").Text();
            var parts = text.Split('.');
            var sport = parts.First().Trim();

            sport = BM.Core.Helper.ConvertSport(sport);

            return sport;
        }

        public static Event CreateEvent(IElement element)
        {
            var @event = new Event();

            var date = GetNodeFirstText(element.QuerySelector("tr.bk > td:nth-child(1)"));

            var teams = GetNodeChildTextValues(element.QuerySelector("tr.bk > td.l"), x => x.NodeType == NodeType.Text || x.NodeName == "SMALL");

            if (teams.Length != 2)
            {
                return null;
            }

            @event.Home = teams.First();
            @event.Away = teams.Last();

            var score = GetNodeFirstText(element.QuerySelector("tr.bk > td.l > span.l"));

            var scores = ScoreRegex.Match(score);

            if (!scores.Success)
            {
                return null;
            }

            @event.Score1 = scores.Groups["score1"].Value;
            @event.Score2 = scores.Groups["score2"].Value;

            @event.PeriodScores = scores.Groups["pscores"].Value.Split(',');

            @event.Date = date.ToDateTime();

            return @event;
        }

        public static string GetNodeFirstText(INode node)
        {
            var values = GetNodeChildTextValues(node);

            return values.FirstOrDefault() ?? string.Empty;
        }

        public static string[] GetNodeChildTextValues(INode node)
        {
            return GetNodeChildTextValues(node, x => x.NodeType == NodeType.Text);
        }

        public static string[] GetNodeChildTextValues(INode node, Func<INode, bool> predicat)
        {
            if (node == null)
            {
                return new string[0];
            }

            var values = node
                .ChildNodes
                .Where(predicat)
                .Select(x => x.Text().Trim())
                .Where(x => x.Length > 0)
                .ToArray();

            return values;
        }
    }

    public static class EventBlocksParserHelper
    {
        private const int EVENT_COLUMN_NAME_INDEX = 1;

        public static Dictionary<string, int> OutcomeOffset = new Dictionary<string, int>
        {
            { "Odds", -1 },
            { "Over", -1 },
            { "Under", -2 },
        };

        public static void LoadBlocksByHeader(Event @event, IElement element, string[] eventHeaderNames)
        {
            var elements = element.QuerySelectorAll("tr.bk");

            var periodName = string.Empty;

            for (var i = 0; i < elements.Length; i++)
            {
                var betElements = BlocksParserHelper.GetBetElements(elements[i]);

                var name = betElements[EVENT_COLUMN_NAME_INDEX].Text().Trim();

                if (!string.IsNullOrEmpty(name))
                {
                    periodName = name.Trim('.', ':');
                }

                var betBlock = new BetBlock("Main Bets", @event) { PriodName = periodName };

                LoadBlockByHeader(betBlock, betElements, eventHeaderNames);

                if (betBlock.Outcomes.Any())
                {
                    @event.BetBlocks.Add(betBlock);
                }
            }
        }

        public static void LoadBlockByHeader(BetBlock betBlock, IElement[] betElements, string[] eventHeaderNames)
        {
            if (betElements.Length != eventHeaderNames.Length)
            {
                return;
            }

            for (var i = 0; i < eventHeaderNames.Length; i++)
            {
                var outcomes = betElements[i]?.QuerySelectorAll("u > a");

                if (outcomes == null || outcomes.Length == 0)
                {
                    continue;
                }

                var headerName = eventHeaderNames[i];

                int offset;
                decimal[] outcomeParams = null;
                if (OutcomeOffset.TryGetValue(headerName, out offset))
                {
                    headerName = $"{eventHeaderNames[i + offset]}{headerName}";
                    outcomeParams = BlocksParserHelper.GetOutcomeParams(betElements[i + offset]);
                }

                if (outcomeParams != null && outcomeParams.Length != outcomes.Length)
                {
                    continue;
                }

                for (var o = 0; o < outcomes.Length; o++)
                {
                    var outcomeElement = outcomes[o];

                    var outcome = BlocksParserHelper.CreateOutcome(outcomeElement);

                    outcome.SetName(outcomes.Length > 1 ? $"{headerName}{o + 1}" : headerName);

                    outcome.Param = outcomeParams?[o];

                    betBlock.AddOutcome(outcome);
                }
            }
        }

        public static void LoadDinBlocks(Event @event, IElement propsElement)
        {
            var elements = propsElement.QuerySelectorAll("tr > td.dyn");

            var length = elements.Length;
            for (var i = 0; i < length; i++)
            {
                var element = elements[i];
                var name = element.QuerySelector("i.p2r").Text().Trim(':');

                var betBlock = new BetBlock(name, @event);

                BlocksParserHelper.LoadNobrs(betBlock, element);

                if (betBlock.Outcomes.Any())
                {
                    @event.BetBlocks.Add(betBlock);
                }
            }
        }

        public static void LoadP2rBlocks(Event @event, IElement propsElement)
        {
            var elements = propsElement.QuerySelectorAll("tr > td.p2r");

            var length = elements.Length;
            for (var i = 0; i < length; i++)
            {
                var element = elements[i];
                var name = element.QuerySelector("i.p2r").Text().Trim(':');

                var betBlock = new BetBlock(name, @event);

                var outcomeElements = element.QuerySelectorAll("u > a");

                foreach (var outcomeElement in outcomeElements)
                {
                    if (outcomeElement.Parent.PreviousSibling.NodeType == NodeType.Text)
                    {
                        var outcome = BlocksParserHelper.CreateOutcome(outcomeElement);
                        outcome.SetName(outcomeElement.Parent.PreviousSibling.Text());
                        betBlock.AddOutcome(outcome);
                    }
                }

                if (betBlock.Outcomes.Any())
                {
                    @event.BetBlocks.Add(betBlock);
                }
            }
        }

        private static readonly Regex PsBlockRegex = new Regex(@"^(?:[(](?<param>[\d+-–,.]+)[)])?(?:\s{0,}(?<name>over|under))?$", RegexOptions.Compiled);
        public static void LoadPsBlocks(Event @event, IElement propsElement)
        {
            var psElements = propsElement.QuerySelectorAll("tr > td > table.ps > tbody > tr.btb");

            var length = psElements.Length;
            for (var i = 0; i < length; i++)
            {
                var psElement = psElements[i];
                var blockName = psElement.QuerySelector("th").Text().Trim(':');

                var betBlock = new BetBlock(blockName, @event);

                var betElements = psElement.NextElementSibling.QuerySelector("tr > td > table.ps > tbody");

                foreach (var betElement in betElements.Children)
                {
                    var name = betElement.QuerySelector("td > i")?.Text().Trim();

                    var outcomeElements = betElement.QuerySelectorAll("u > a");

                    decimal? param = null;
                    foreach (var outcomeElement in outcomeElements)
                    {
                        if (outcomeElement.Parent.PreviousSibling.NodeType == NodeType.Text)
                        {
                            var outcomeName = outcomeElement.Parent.PreviousSibling.Text().Trim(',', ' ', ';');

                            var match = PsBlockRegex.Match(outcomeName);

                            if (match.Success)
                            {
                                var outcome = BlocksParserHelper.CreateOutcome(outcomeElement);
                                outcome.Param = match.Groups["param"].ToNullDecimal();

                                if (param.HasValue && !outcome.Param.HasValue)
                                {
                                    outcome.Param = param;
                                }
                                else
                                {
                                    param = outcome.Param;
                                }


                                outcome.SetName($"{name}{match.Groups["name"].Value}");

                                betBlock.AddOutcome(outcome);
                            }
                        }
                    }
                }

                if (betBlock.Outcomes.Any())
                {
                    @event.BetBlocks.Add(betBlock);
                }
            }
        }
    }

    public static class BlocksParserHelper
    {
        private const string COLSPAN_ATTRIBUTE = "colspan";
        private static readonly Regex NobrRegex = new Regex(@"^(?<name>over|under) [(](?<param>[\d+-–,.]+)[)]$", RegexOptions.Compiled);

        public static IElement[] GetBetElements(IElement element)
        {
            var betElements = element.QuerySelectorAll("tr.bk > td");

            var result = new List<IElement>();

            foreach (var betElement in betElements)
            {
                if (betElement.HasAttribute(COLSPAN_ATTRIBUTE))
                {
                    var colspan = betElement.GetAttribute(COLSPAN_ATTRIBUTE).ToInt();

                    result.AddRange(new IElement[colspan]);
                }
                else
                {
                    result.Add(betElement);
                }
            }

            return result.ToArray();
        }

        public static decimal[] GetOutcomeParams(IElement element)
        {
            var outcomeParamElements = element.QuerySelectorAll("b");

            var outcomeParams = new List<decimal>();

            foreach (var outcomeElement in outcomeParamElements)
            {
                var textValue = outcomeElement.Text().Replace("–", "-");
                var value = textValue.ToNullDecimal();

                if (value.HasValue)
                {
                    outcomeParams.Add(value.Value);
                }
            }

            return outcomeParams.ToArray();
        }

        public static void LoadNobrs(BetBlock betBlock, IElement dynBlockElement)
        {
            var nobrs = dynBlockElement.QuerySelectorAll("nobr");

            var length = nobrs.Length;
            for (var i = 0; i < length; i++)
            {
                var nobrElement = nobrs[i];
                var name = ParserHelper.GetNodeFirstText(nobrElement);

                var outcomeElement = nobrElement.QuerySelector("u > a");

                var outcome = CreateOutcome(outcomeElement);

                var match = NobrRegex.Match(name);

                if (match.Success)
                {
                    name = match.Groups["name"].Value;
                    outcome.Param = match.Groups["param"].Value.ToNullDecimal();
                }

                outcome.SetName(name.Trim());

                betBlock.AddOutcome(outcome);
            }
        }

        public static Outcome CreateOutcome(IElement outcomeElement)
        {
            var value = outcomeElement.Text();
            var outcomeId = Regex.Replace(outcomeElement.Id, ".*?_.*?_", "");

            var outcome = new Outcome
            {
                Id = outcomeId,
                Value = value.ToDecimal()
            };

            return outcome;
        }
    }
}