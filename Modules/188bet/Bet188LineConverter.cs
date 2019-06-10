using System;
using System.Collections.Generic;
using System.Linq;
using Bars.EAS.Utils;
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
                    //CoeffType = @event.event_result_name.ToLower(),
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

            return localLines;
        }

        /// <param name="k">EventId</param>
        private void Convert1x2(LineDTO template, o o, string k)
        {
            LineDTO line;
         

            if (o._1x2 != null)
            {
                line = template.Clone();
                line.CoeffKind = "1";
                line.CoeffValue = o._1x2.v[1].ToDecimal();
                line.LineData = k + ";" + o._1x2.v[0].Substring(1); //; handicap

                line.UpdateName();
                localLines.Add(line);

                line = template.Clone();
                line.CoeffKind = "2";
                line.CoeffValue = o._1x2.v[3].ToDecimal();
                line.LineData = k + ";" + o._1x2.v[2].Substring(1);

                line.UpdateName();
                localLines.Add(line);

                line = template.Clone();
                line.CoeffKind = "X";
                line.CoeffValue = o._1x2.v[5].ToDecimal();
                line.LineData = k + ";" + o._1x2.v[4].Substring(1);

                line.UpdateName();
                localLines.Add(line);
            }

            if (o._1x21st != null)
            {
                line = template.Clone();
                line.CoeffKind = "1";
                line.CoeffValue = o._1x21st.v[1].ToDecimal();
                line.LineData = k + ";" + o._1x21st.v[0].Substring(1);
                line.CoeffType = "1st half";

                line.UpdateName();
                localLines.Add(line);

                line = template.Clone();
                line.CoeffKind = "2";
                line.CoeffValue = o._1x21st.v[3].ToDecimal();
                line.LineData = k + ";" + o._1x21st.v[2].Substring(1);
                line.CoeffType = "1st half";

                line.UpdateName();
                localLines.Add(line);

                line = template.Clone();
                line.CoeffKind = "X";
                line.CoeffValue = o._1x21st.v[5].ToDecimal();
                line.LineData = k + ";" + o._1x21st.v[4].Substring(1);
                line.CoeffType = "1st half";

                line.UpdateName();
                localLines.Add(line);
            }



        }
    }

}


