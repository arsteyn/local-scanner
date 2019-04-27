using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Parser.Html;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using log4net;
using Newtonsoft.Json;
using Pinnacle.JsonClasses;

namespace Pinnacle
{
    public class PinnacleLineConverter
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(PinnacleLineConverter));

        private List<LineDTO> _lines;

        private string _bookmakerName;

        //private static readonly string[] Types = { "Yellow Cards", "YC", "Offsides", "Shots on goal", "Corners" };

        private static readonly Regex ScoreRegex = new Regex("^(?<homeScore>.*?)‑(?<awayScore>.*?)$");

        private static readonly HtmlParser HtmlParser = new HtmlParser();

        public LineDTO[] Convert(string value, string bookmakerName)
        {
            _bookmakerName = bookmakerName;

            _lines = new List<LineDTO>();

            if (value == null) return new LineDTO[] { };

            var parsedResult = JsonConvert.DeserializeObject<RequestResult>(value);

            var containers = parsedResult.Sport.Markets.SelectMany(m => m.GamesContainers);

            var lineTemplateDto = new LineDTO
            {
                SportKind = Helper.ConvertSport(parsedResult.Sport.SportName),
                BookmakerName = _bookmakerName,
                CoeffType = parsedResult.Sport.Periods.First(p => p.IsSelected).Id != 0 ? parsedResult.Sport.Periods.First(p => p.IsSelected).Text : string.Empty,
                ObjectCreateDate = DateTime.Now
            };

            foreach (var container in containers)
            {
                Convert(container.Value, lineTemplateDto);
            }

            return _lines.ToArray();
        }

        private void Convert(GamesContainer container, LineDTO lineTemplateDto)
        {
            foreach (var gameLine in container.GameLines)
            {
                Convert(gameLine, lineTemplateDto);
            }
        }

        readonly List<string> _stopWords = new List<string> { "pen)" };

        private void Convert(GameLine gameLine, LineDTO lineTemplateDto)
        {
            lineTemplateDto = lineTemplateDto.Clone();

            lineTemplateDto.EventDate = gameLine.Date;

            if (_stopWords.Any(sw => gameLine.TmH.Txt.ContainsIgnoreCase(sw) || gameLine.TmA.Txt.ContainsIgnoreCase(sw))) return;

            lineTemplateDto.Team1 = gameLine.TmH.Txt;

            lineTemplateDto.Team2 = gameLine.TmA.Txt;

            try
            {
                var teamScore = ScoreRegex.Match(gameLine.DispDate.Split('>')[1]);

                lineTemplateDto.Score1 = System.Convert.ToInt32(teamScore.Groups["homeScore"].Value.Trim());

                lineTemplateDto.Score2 = System.Convert.ToInt32(teamScore.Groups["awayScore"].Value.Trim());

                ConvertHandicap(gameLine, lineTemplateDto);
            }
            catch (Exception e)
            {
                Log.Error($"Pinnacle score convert ERROR {e.Message}{e.InnerException}{e.StackTrace}");
            }

            ConvertTotal(gameLine, lineTemplateDto);

            ConvertMoneyLine(gameLine, lineTemplateDto);
        }

        private void ConvertMoneyLine(GameLine gameLine, LineDTO lineTemplateDto)
        {
            BuildMoneyLine(gameLine, lineTemplateDto, "1");

            BuildMoneyLine(gameLine, lineTemplateDto, "X");

            BuildMoneyLine(gameLine, lineTemplateDto, "2");
        }

        private void ConvertHandicap(GameLine gameLine, LineDTO lineTemplateDto)
        {
            BuildHandicapLine(gameLine, "HANDICAP1", lineTemplateDto);

            BuildHandicapLine(gameLine, "HANDICAP2", lineTemplateDto);
        }

        private void ConvertTotal(GameLine gameLine, LineDTO lineTemplateDto)
        {
            BuilTotalLine(gameLine, "TOTALOVER", lineTemplateDto);

            BuilTotalLine(gameLine, "TOTALUNDER", lineTemplateDto);
        }

        private void BuilTotalLine(GameLine gameLine, string kind, LineDTO lineTemplateDto)
        {
            var line = lineTemplateDto.Clone();

            line.CoeffKind = kind;

            var i = 0;

            switch (kind)
            {
                case "TOTALOVER":
                    i = 1;
                    break;
                case "TOTALUNDER":
                    i = 2;
                    break;
            }

            if (gameLine.TotWagerMap[i] == "" || HtmlParser.Parse(gameLine.TotWagerMap[i]).DocumentElement.QuerySelector("a").InnerHtml == "&nbsp;")
            {
                return;
            }

            var url = HtmlParser.Parse(gameLine.TotWagerMap[i]).DocumentElement.QuerySelector("a").GetAttribute("href");

            var idx = url.IndexOf('?');
            var query = idx >= 0 ? url.Substring(idx) : "";
            var lineParameterValue = HttpUtility.ParseQueryString(query).Get("line");

            decimal.TryParse(lineParameterValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var coeffParam);
            line.CoeffParam = coeffParam;

            decimal.TryParse(HtmlParser.Parse(gameLine.TotWagerMap[i]).DocumentElement.QuerySelector("a").InnerHtml, NumberStyles.Any, CultureInfo.InvariantCulture, out var coeffValue);
            line.CoeffValue = coeffValue;

            line.LineObject = url;

            AddLine(line);
        }

        private void BuildHandicapLine(GameLine gameLine, string kind, LineDTO lineTemplateDto)
        {
            var line = lineTemplateDto.Clone();

            line.CoeffKind = kind;

            var i = 0;

            switch (kind)
            {
                case "HANDICAP1":
                    i = 1;
                    break;
                case "HANDICAP2":
                    i = 3;
                    break;
            }

            if (gameLine.SpWagerMap[i] == "" || HtmlParser.Parse(gameLine.SpWagerMap[i]).DocumentElement.QuerySelector("a").InnerHtml == "&nbsp;")
            {
                return;
            }

            var url = HtmlParser.Parse(gameLine.SpWagerMap[i]).DocumentElement.QuerySelector("a").GetAttribute("href");

            var idx = url.IndexOf('?');
            var query = idx >= 0 ? url.Substring(idx) : "";
            var lineParameterValue = HttpUtility.ParseQueryString(query).Get("line");

            decimal.TryParse(lineParameterValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var coeffParam);
            line.CoeffParam = coeffParam;

            decimal.TryParse(HtmlParser.Parse(gameLine.SpWagerMap[i]).DocumentElement.QuerySelector("a").InnerHtml, NumberStyles.Any, CultureInfo.InvariantCulture, out var coeffValue);
            line.CoeffValue = coeffValue;

            line.LineObject = url;

            AddLine(line);
        }

        private void BuildMoneyLine(GameLine gameLine, LineDTO lineTemplateDto, string coeffKind)
        {
            var line = lineTemplateDto.Clone();
            line.CoeffKind = coeffKind;

            var i = 0;
            switch (coeffKind)
            {
                case "1":
                    i = 1;
                    break;
                case "X":
                    i = 0;
                    break;
                case "2":
                    i = 2;
                    break;
            }

            if (gameLine.WagerMap[i] == "" || HtmlParser.Parse(gameLine.WagerMap[i]).DocumentElement.QuerySelector("a").InnerHtml == "&nbsp;")
            {
                return;
            }

            decimal.TryParse(HtmlParser.Parse(gameLine.WagerMap[i]).DocumentElement.QuerySelector("a").InnerHtml, NumberStyles.Any, CultureInfo.InvariantCulture, out var coeffValue);
            line.CoeffValue = coeffValue;

            var url = HtmlParser.Parse(gameLine.WagerMap[i]).DocumentElement.QuerySelector("a").GetAttribute("href");

            line.LineObject = url;

            AddLine(line);
        }



        private void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            _lines.Add(lineDto);
        }

    }
}


