using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bars.EAS.Utils;
using HtmlAgilityPack;

namespace Parimatch.AdditionalClasses
{
    public class LinesInfo
    {
        public DateTime Date { get; set; }

        public string FirstSide { get; set; }

        public string SecondSide { get; set; }

        public string SportKind { get; set; }

        public int Score1 { get; set; }
        public int Score2 { get; set; }
        public int? Pscore1 { get; set; }
        public int? Pscore2 { get; set; }

        public IList<string> Headers { get; set; }

        public void LoadHeader(HtmlNode htmlNode)
        {
            var header = htmlNode.SelectSingleNode("tr");
            Headers = header == null ? null : header.ChildNodes.Select(x => x.InnerText.ToUpper().Replace("HAND.", "HANDICAP")).ToList();
        }

        public string BookmakerName { get; set; }

        public HtmlNode[] EventColumns { get; set; }

        private readonly Regex _scoreRegex = new Regex(@"^(?<score1>\d+)-(?<score2>\d+)(?:.*?[(].*?(?<pscore1>\d+)-(?<pscore2>\d+)[)])?(?: \d+')?");

        public void LoadInfo(HtmlNode eventRow)
        {
            var eventLine = eventRow.SelectSingleNode("tr[@class='bk']");

            if (eventLine == null)
            {
                return;
            }

            var events = eventLine.SelectNodes("td");

            var time = TimeSpan.Parse(events[Headers.IndexOf("TIME")].SelectSingleNode("text()").InnerText);

            SetToDate(time);

            var eventBlock = events[Headers.IndexOf("EVENT")];
            var teamBlocks = eventBlock.SelectNodes("text()");
            var scoreBlock = eventBlock.SelectSingleNode("span");

            if (teamBlocks != null && teamBlocks.Count == 2)
            {
                FirstSide = teamBlocks.First().InnerText;
                SecondSide = teamBlocks.Last().InnerText;
            }
            else
            {
                FirstSide = eventBlock.ChildNodes[0].InnerText;
                SecondSide = eventBlock.ChildNodes[2].InnerText;
            }

            if (scoreBlock != null)
            {
                var match = _scoreRegex.Match(scoreBlock.InnerText);
                Score1 = match.Groups["score1"].Value.ToInt();
                Score2 = match.Groups["score2"].Value.ToInt();
                Pscore1 = match.Groups["pscore1"].Value.ToIntNullable();
                Pscore2 = match.Groups["pscore2"].Value.ToIntNullable();
            }
        }

        public void LoadBk(HtmlNode eventLine)
        {
            var elements = eventLine.SelectNodes("td").ToArray();

            EventColumns = new HtmlNode[Headers.Count];
            var valueIndex = 0;
            for (int index = 0; index < Headers.Count; index++)
            {
                if (valueIndex >= elements.Count())
                {
                    break;
                }

                var value = elements[valueIndex];

                if (value.HasAttributes && value.Attributes["colspan"] != null)
                {
                    var count = int.Parse(value.Attributes["colspan"].Value);

                    if (count > 1)
                    {
                        index += count - 1;
                        valueIndex++;
                        continue;
                    }
                }

                EventColumns[index] = value;
                valueIndex++;
            }
        }

        public void SetToDate(TimeSpan timeSpan)
        {
            this.Date = DateTime.Now.Date.Add(timeSpan);
        }

        public void Load(HtmlNode eventRow)
        {
            var eventLine = eventRow.SelectSingleNode("tr[@class='bk']");

            if (eventLine == null)
            {
                EventColumns = null;
                return;
            }

            LoadBk(eventLine);
        }
    }
}
