using System;
using System.Collections.Generic;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using Dafabet.Models;

namespace Dafabet
{
    public class DafabetConverter
    {
        private List<LineDTO> _lines;

        public LineDTO[] Convert(MatchDataResult data, string bookmakerName)
        {
            _lines = new List<LineDTO>();

            foreach (var league in data.leagues)
            {
                foreach (var match in league.matches)
                {
                    var lineTemplate = new LineDTO();

                    lineTemplate.SportKind = Helper.ConvertSport(league.SportName);
                    lineTemplate.BookmakerName = bookmakerName;

                    lineTemplate.Team1 = match.HomeName;
                    lineTemplate.Team2 = match.AwayName;

                    lineTemplate.Score1 = match.MoreInfo.ScoreH;
                    lineTemplate.Score2 = match.MoreInfo.ScoreA;

                    foreach (var oddSet in match.oddset)
                    {
                        var lineTemplate2 = lineTemplate.Clone();

                        lineTemplate2.CoeffType = GetCoeffType(oddSet.Bettype);

                        foreach (var setSel in oddSet.sels)
                        {
                            var lineTemplate3 = lineTemplate2.Clone();

                            var coeffKind = GetCoeffKind(oddSet.Bettype, setSel.Key, out var hasParam);

                            //ProxyHelper.UpdateDafabetEvents($"SportName {league.SportName} | SportType {league.SportType} | Bettype {oddSet.Bettype} | OddsId {oddSet.OddsId} | Key Point Price {setSel.Key} {setSel.Point} {setSel.Price}");

                            if (coeffKind.IsEmpty()) continue;

                            lineTemplate3.CoeffKind = coeffKind;

                            if (hasParam) lineTemplate3.CoeffParam = coeffKind == "HANDICAP1" ? -1 * setSel.Point : setSel.Point;

                            lineTemplate3.CoeffValue = ConvertToDecimalOdds(setSel.Price);

                            lineTemplate3.LineObject = $"{oddSet.OddsId}|{setSel.Key}|{oddSet.Bettype}|{setSel.Price}";

                            AddLine(lineTemplate3);
                        }
                    }
                }
            }

            return _lines.ToArray();
        }

        private string GetCoeffType(int betType)
        {
            switch (betType)
            {
                case 7:
                case 8:
                case 12:
                case 15:
                case 191:
                case 410:
                case 411:
                    return "1st half";
                case 17:
                case 18:
                case 177:
                case 178:
                case 183:
                case 185:
                case 186:
                case 428:
                case 430:
                case 431:
                case 432:
                    return "2nd half";
                default:
                    return null;
            }
        }

        private static string GetCoeffKind(int betType, string betteam, out bool hasParam)
        {
            hasParam = false;
            switch (betType)
            {
                #region Handicap

                //HANDICAP
                case 1:
                //1H HANDICAP
                case 7:
                //2H Handicap //TODO:проверить параметры
                case 17:
                case 183:
                    hasParam = true;
                    switch (betteam)
                    {
                        case "h":
                            return "HANDICAP1";
                        case "a":
                            return "HANDICAP2";
                        default:
                            return string.Empty;
                    }

                #endregion

                #region Over/Under

                //FT OVER/UNDER
                case 3:
                //1H OVER/UNDER
                case 8:
                //2H OVER/UNDER
                case 18:
                case 178:
                    hasParam = true;
                    switch (betteam)
                    {
                        case "h":
                        case "o":
                            return "TOTALOVER";
                        case "a":
                        case "u":
                            return "TOTALUNDER";
                        default:
                            return string.Empty;
                    }

                #endregion

                #region 1X2

                //1X2
                case 5:
                //1H 1X2
                case 15:
                //2H 1X2
                case 430:
                case 177:

                    switch (betteam)
                    {
                        case "1":
                            return "1";
                        case "x":
                            return "x";
                        case "2":
                            return "2";
                        default:
                            return string.Empty;
                    }
                  

                #endregion

                #region Double chance

                //1H Double chance
                case 410:
                //2H Double chance
                case 186:
                case 431:
                //Double chance
                case 24:

                    switch (betteam)
                    {
                        case "hd":
                        case "1x":
                            return "1x";
                        case "ha":
                        case "12":
                            return "12";
                        case "da":
                        case "2x":
                            return "x2";
                        default:
                            return string.Empty;
                    }

                #endregion

                #region Draw No Bet

                //Draw no bet
                case 25:
                //1H Draw No Bet
                case 411:
                //2H Draw No Bet
                case 432:
                case 185:
                case 191:

                    switch (betteam)
                    {
                        case "h":
                            return "W1";
                        case "a":
                            return "W2";
                        default:
                            return string.Empty;
                    }

                    #endregion
            }

            return string.Empty;
        }

        private decimal ConvertToDecimalOdds(decimal my)
        {
            decimal price;

            if (my > 0 && my <= 1)
                price = my + 1m;
            else if (my >= -1 && my < 0)
                price = -1m / my + 1m;
            else
                price = my;

            return decimal.Round(price, 2, MidpointRounding.AwayFromZero); ;
        }

        private void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            _lines.Add(lineDto);
        }

    }
}


