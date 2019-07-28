using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bars.EAS.Utils;
using Bars.EAS.Utils.Extension;
using Bet188.Models;
using BM;
using BM.DTO;
using Newtonsoft.Json;
using NLog;

namespace Bet188
{
    public class Bet188LineConverter
    {
        protected Logger Log => LogManager.GetCurrentClassLogger();

        public LineDTO CreateLine(EventDto @event, string host, string name)
        {
            try
            {

                var line = new LineDTO
                {
                    BookmakerName = name,
                    SportKind = @event.Sport,
                    Team1 = @event.Event.ht,
                    Team2 = @event.Event.at,
                    Url = $"/en-gb/sports/{@event.Event.eid}/in-play/{@event.Event.en}",
                    Score1 = @event.Event.hs.v,
                    Score2 = @event.Event.As.v,
                    EventDate = DateTime.Now,
                    LineObject = JsonConvert.SerializeObject(@event.Event)
                };

                return line;
            }
            catch (Exception e)
            {
                Log.Info($"{name} CreateLine exception " + JsonConvert.SerializeObject(e) + Environment.NewLine + JsonConvert.SerializeObject(@event));
            }

            return null;
        }

        private List<LineDTO> localLines;
        public List<LineDTO> GetLinesFromEvent(LineDTO template, CentralServiceResult result)
        {
            localLines = new List<LineDTO>();

            var e = result.mbd.d.c[0].e[0];

            if (e.hide) return localLines;

            Convert1x2(template, e.o, e.k);
            ConvertHandicap(template, e.o, e.k);
            ConvertTotal(template, e.o, e.k);

            foreach (var n_o in e.n_o)
            {
                switch (n_o.n)
                {
                    case "Double Chance":
                        ConvertDoubleChance(template, n_o.o, e.k);
                        break;
                }
            }

            foreach (var cel in e.cel)
            {
                ConvertIndividualTotal(template, cel.o, e.k);
            }

            return localLines;
        }

        private void ConvertIndividualTotal(LineDTO template, o o, string k)
        {
            LineDTO line;

            if (o.ou != null)
            {
                string team = null;

                if (o.ou.n.EqualsIgnoreCase($"Team Goals: {template.Team1} -  Over / Under"))
                    team = "1";
                else if (o.ou.n.EqualsIgnoreCase($"Team Goals: {template.Team2} -  Over / Under"))
                    team = "2";

                if (team != null)
                {
                    var chunks = o.ou.v
                        .Select((s, i) => new { Value = s, Index = i })
                        .GroupBy(x => x.Index / 8)
                        .Select(grp => grp.Select(x => x.Value).ToArray())
                        .ToArray();

                    foreach (var chunk in chunks)
                    {
                        line = template.Clone();
                        line.CoeffKind = $"ITOTALOVER{team}";
                        line.CoeffValue = chunk[5].ToDecimal();
                        line.CoeffParam = ConvertParam(chunk[1]);
                        line.LineData = k + ";" + chunk[4].Substring(1) + ";" + chunk[1];

                        line.UpdateName();
                        AddLine(line);

                        line = template.Clone();
                        line.CoeffKind = $"ITOTALUNDER{team}";
                        line.CoeffValue = chunk[7].ToDecimal();
                        line.CoeffParam = ConvertParam(chunk[3]);
                        line.LineData = k + ";" + chunk[6].Substring(1) + ";" + chunk[3];

                        line.UpdateName();
                        AddLine(line);
                    }
                }
            }

            if (o.ou1st != null)
            {
                string team = null;

                if (o.ou1st.n.EqualsIgnoreCase($"Team Goals: {template.Team1} -  Over / Under - 1st Half"))
                    team = "1";
                else if (o.ou1st.n.EqualsIgnoreCase($"Team Goals: {template.Team2} -  Over / Under - 1st Half"))
                    team = "2";

                if (team != null)
                {

                    var chunks = o.ou1st.v
                        .Select((s, i) => new { Value = s, Index = i })
                        .GroupBy(x => x.Index / 8)
                        .Select(grp => grp.Select(x => x.Value).ToArray())
                        .ToArray();

                    foreach (var chunk in chunks)
                    {
                        line = template.Clone();
                        line.CoeffKind = $"ITOTALOVER{team}";
                        line.CoeffValue = chunk[5].ToDecimal();
                        line.CoeffParam = ConvertParam(chunk[1]);
                        line.CoeffType = "1st half";
                        line.LineData = k + ";" + chunk[4].Substring(1) + ";" + chunk[1];

                        line.UpdateName();
                        AddLine(line);

                        line = template.Clone();
                        line.CoeffKind = $"ITOTALUNDER{team}";
                        line.CoeffValue = chunk[7].ToDecimal();
                        line.CoeffParam = ConvertParam(chunk[3]);
                        line.CoeffType = "1st half";
                        line.LineData = k + ";" + chunk[6].Substring(1) + ";" + chunk[3];

                        line.UpdateName();
                        AddLine(line);
                    }
                }
            }
        }

        private void ConvertTotal(LineDTO template, o o, string k)
        {
            LineDTO line;

            if (o.ou != null)
            {
                var chunks = o.ou.v
                    .Select((s, i) => new { Value = s, Index = i })
                    .GroupBy(x => x.Index / 8)
                    .Select(grp => grp.Select(x => x.Value).ToArray())
                    .ToArray();

                foreach (var chunk in chunks)
                {
                    line = template.Clone();
                    line.CoeffKind = "TOTALOVER";
                    line.CoeffValue = chunk[5].ToDecimal();
                    line.CoeffParam = ConvertParam(chunk[1]);
                    line.LineData = k + ";" + chunk[4].Substring(1) + ";" + chunk[1];

                    line.UpdateName();
                    AddLine(line);

                    line = template.Clone();
                    line.CoeffKind = "TOTALUNDER";
                    line.CoeffValue = chunk[7].ToDecimal();
                    line.CoeffParam = ConvertParam(chunk[3]);
                    line.LineData = k + ";" + chunk[6].Substring(1) + ";" + chunk[3];

                    line.UpdateName();
                    AddLine(line);
                }
            }

            if (o.ou1st != null)
            {
                var chunks = o.ou1st.v
                    .Select((s, i) => new { Value = s, Index = i })
                    .GroupBy(x => x.Index / 8)
                    .Select(grp => grp.Select(x => x.Value).ToArray())
                    .ToArray();

                foreach (var chunk in chunks)
                {
                    line = template.Clone();
                    line.CoeffKind = "TOTALOVER";
                    line.CoeffValue = chunk[5].ToDecimal();
                    line.CoeffParam = ConvertParam(chunk[1]);
                    line.CoeffType = "1st half";
                    line.LineData = k + ";" + chunk[4].Substring(1) + ";" + chunk[1];

                    line.UpdateName();
                    AddLine(line);

                    line = template.Clone();
                    line.CoeffKind = "TOTALUNDER";
                    line.CoeffValue = chunk[7].ToDecimal();
                    line.CoeffParam = ConvertParam(chunk[3]);
                    line.CoeffType = "1st half";
                    line.LineData = k + ";" + chunk[6].Substring(1) + ";" + chunk[3];

                    line.UpdateName();
                    AddLine(line);
                }
            }
        }

        private void ConvertHandicap(LineDTO template, o o, string k)
        {
            LineDTO line;

            //Handicap
            if (o.ah != null)
            {
                var chunks = o.ah.v
                    .Select((s, i) => new { Value = s, Index = i })
                    .GroupBy(x => x.Index / 8)
                    .Select(grp => grp.Select(x => x.Value).ToArray())
                    .ToArray();

                foreach (var chunk in chunks)
                {
                    line = template.Clone();
                    line.CoeffKind = "HANDICAP1";
                    line.CoeffValue = chunk[5].ToDecimal();
                    line.CoeffParam = ConvertParam(chunk[1]);
                    line.LineData = k + ";" + chunk[4].Substring(1) + ";" + chunk[1];

                    line.UpdateName();
                    AddLine(line);

                    line = template.Clone();
                    line.CoeffKind = "HANDICAP2";
                    line.CoeffValue = chunk[7].ToDecimal();
                    line.CoeffParam = ConvertParam(chunk[3]);
                    line.LineData = k + ";" + chunk[6].Substring(1) + ";" + chunk[3];

                    line.UpdateName();
                    AddLine(line);
                }
            }

            //Handicap 1st half
            if (o.ah1st != null)
            {
                var chunks = o.ah1st.v
                    .Select((s, i) => new { Value = s, Index = i })
                    .GroupBy(x => x.Index / 8)
                    .Select(grp => grp.Select(x => x.Value).ToArray())
                    .ToArray();

                foreach (var chunk in chunks)
                {
                    line = template.Clone();
                    line.CoeffKind = "HANDICAP1";
                    line.CoeffValue = chunk[5].ToDecimal();
                    line.CoeffParam = ConvertParam(chunk[1]);
                    line.CoeffType = "1st half";
                    line.LineData = k + ";" + chunk[4].Substring(1) + ";" + chunk[1];

                    line.UpdateName();
                    AddLine(line);

                    line = template.Clone();
                    line.CoeffKind = "HANDICAP2";
                    line.CoeffValue = chunk[7].ToDecimal();
                    line.CoeffParam = ConvertParam(chunk[3]);
                    line.CoeffType = "1st half";
                    line.LineData = k + ";" + chunk[6].Substring(1) + ";" + chunk[3];

                    line.UpdateName();
                    AddLine(line);
                }
            }

        }

        private void ConvertDoubleChance(LineDTO template, List<List<string>> o, string k)
        {
            LineDTO line;

            var map = new Dictionary<string, string>
            {
                {$"{template.Team1} / Draw".ToLower(), "1X"},
                {$"{template.Team2} / Draw".ToLower(), "X2"},
                {$"{template.Team1} / {template.Team2}".ToLower(), "12"}
            };

            foreach (var oItem in o)
            {
                if (!map.ContainsKey(oItem[0].ToLower())) continue;

                line = template.Clone();
                line.CoeffKind = map[oItem[0].ToLower()];
                line.CoeffValue = oItem[2].ToDecimal();
                line.LineData = k + ";" + oItem[1].Substring(1);
                AddLine(line);
            }
        }

        /// <param name="k">EventId</param>
        private void Convert1x2(LineDTO template, o o, string k)
        {
            LineDTO line;
            //1X2
            if (o._1x2 != null)
            {
                line = template.Clone();
                line.CoeffKind = "1";
                line.CoeffValue = o._1x2.v[1].ToDecimal();
                line.LineData = k + ";" + o._1x2.v[0].Substring(1); //; handicap

                AddLine(line);

                line = template.Clone();
                line.CoeffKind = "2";
                line.CoeffValue = o._1x2.v[3].ToDecimal();
                line.LineData = k + ";" + o._1x2.v[2].Substring(1);

                AddLine(line);

                line = template.Clone();
                line.CoeffKind = "X";
                line.CoeffValue = o._1x2.v[5].ToDecimal();
                line.LineData = k + ";" + o._1x2.v[4].Substring(1);

                AddLine(line);
            }

            //1X2 1st half
            if (o._1x21st != null)
            {
                line = template.Clone();
                line.CoeffKind = "1";
                line.CoeffValue = o._1x21st.v[1].ToDecimal();
                line.LineData = k + ";" + o._1x21st.v[0].Substring(1);
                line.CoeffType = "1st half";

                AddLine(line);

                line = template.Clone();
                line.CoeffKind = "2";
                line.CoeffValue = o._1x21st.v[3].ToDecimal();
                line.LineData = k + ";" + o._1x21st.v[2].Substring(1);
                line.CoeffType = "1st half";

                AddLine(line);

                line = template.Clone();
                line.CoeffKind = "X";
                line.CoeffValue = o._1x21st.v[5].ToDecimal();
                line.LineData = k + ";" + o._1x21st.v[4].Substring(1);
                line.CoeffType = "1st half";

                AddLine(line);
            }

        }

        private decimal ConvertParam(string param)
        {
            var split = param.Split('/');
            if (split.Length != 2) return param.ToDecimal();

            split[0] = split[0].Replace("-", "").Replace("+", "");
            return (param[0] == '-' ? -1m : 1m) * ((decimal.Parse(split[0], CultureInfo.InvariantCulture) + decimal.Parse(split[1], CultureInfo.InvariantCulture)) / 2m);
        }

        private void AddLine(LineDTO lineDto)
        {
            if (lineDto.CoeffValue == 0m) return;

            lineDto.UpdateName();
            localLines.Add(lineDto);
        }
    }

}


