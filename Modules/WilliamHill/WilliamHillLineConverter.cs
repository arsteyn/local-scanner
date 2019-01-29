using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using BM;
using BM.Core;
using BM.DTO;
using BM.Interfaces;
using WilliamHill.SerializableClasses;

namespace WilliamHill
    {
        public class WilliamHillLineConverter : ILineConverter
        {
            readonly Regex _bettingRegex = new Regex(@"^Match Betting Live$|^(?<type>.*?) Betting(?: -)? Live$");

            readonly Regex _dnbMatch = new Regex(@"^(?<type>.*?) Draw No Bet$|^Draw No Bet Live$|^Money Line Live$");

            readonly Regex _totalRegex = new Regex("^(?<type>.*?) Under/Over [0-9.]+ Goals Live$");

            readonly Regex _handicapMatch = new Regex(@"^(?<type>.*?) - Handicap Betting Live$|^(?:Alternative|Match|(?<type>.*?)) Handicap (?:[+]|[-])?[0-9.]+ Live$|^Handicap Betting(?: -)? Live$");

            readonly Regex _timeMatch = new Regex(@"^Match Betting After (?<time>\d+) Mins Live$");

            static XmlSerializer _xmlSerializer;
            public XmlSerializer XmlSerializer => _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(BIR)));

            public LineDTO[] Convert(string response, string bookmakerName)
            {
                var bir = this.Deserialize(response);

                if (bir == null || bir.Event == null || bir.Event.Status != "A")
                {
                    return new LineDTO[] { };
                }

                var lines = new List<LineDTO>();

                foreach (var market in bir.Event.Markets.Where(x => x.Status == "A"))
                {
                    var coef = market.Name;

                    if (_handicapMatch.IsMatch(coef))
                    {
                        if (market.Selections.Length == 2)
                        {
                            lines.AddRange(ConverterHelper.CreateHandicapLines(market, _handicapMatch.Match(coef).Groups["type"].Value));
                        }
                    }
                    else if (_bettingRegex.IsMatch(coef))
                    {
                        lines.AddRange(ConverterHelper.CreateWinLines(market, _bettingRegex.Match(coef).Groups["type"].Value));
                    }
                    else if (_dnbMatch.IsMatch(coef))
                    {
                        lines.AddRange(ConverterHelper.CreateDnbLines(market, _dnbMatch.Match(coef).Groups["type"].Value));
                    }
                    else if (_timeMatch.IsMatch(coef))
                    {
                        var mins = _timeMatch.Match(coef).Groups["time"].Value;
                        lines.AddRange(ConverterHelper.CreateWinLines(market, $"After {mins} Min"));
                    }
                    else if (_totalRegex.IsMatch(coef))
                    {
                        lines.AddRange(ConverterHelper.CreateTotalLines(market, _totalRegex.Match(coef).Groups["type"].Value));
                    }
                }

                var sport = bir.Event.EventClass.Name;

                lines.ForEach(x =>
                {
                    x.BookmakerName = bookmakerName;
                    x.SportKind = Helper.ConvertSport(sport);
                    x.UpdateName();
                });

                return lines.ToArray();
            }

            private BIR Deserialize(string response)
            {
                if (string.IsNullOrEmpty(response))
                {
                    return null;
                }

                using (var sr = new StringReader(response))
                using (var xr = XmlReader.Create(sr, new XmlReaderSettings { XmlResolver = null, ProhibitDtd = false }))
                {
                    try
                    {
                        if (this.XmlSerializer.CanDeserialize(xr))
                        {
                            return this.XmlSerializer.Deserialize(xr) as BIR;
                        }
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }

                    return null;
                }
            }
        }
    }


