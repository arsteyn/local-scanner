using System;
using System.Collections.Generic;
using BM;
using BM.Core;
using BM.DTO;
using Dafabet.Models;

namespace Dafabet
{
    public class DafabetConverter
    {
        private List<LineDTO> _lines;

        private string GetSportnameFromType(int type)
        {
            switch (type)
            {
                case 1:
                    return "Soccer";
            }

            return string.Empty;
        }

        public LineDTO[] Convert(List<Match> matches, string bookmakerName)
        {
            _lines = new List<LineDTO>();

            foreach (var match in matches)
            {
                if (match.eventstatus != "running") continue;

                var lineTemplate = new LineDTO();

                lineTemplate.SportKind = Helper.ConvertSport(GetSportnameFromType(match.sporttype));
                lineTemplate.BookmakerName = bookmakerName;

                lineTemplate.Team1 = match.hteamnameen;
                lineTemplate.Team2 = match.ateamnameen;

                lineTemplate.Score1 = match.livehomescore;
                lineTemplate.Score2 = match.liveawayscore;

                foreach (var oddSet in match.oddset.Values)
                {
                    if (oddSet.oddsstatus != "running") continue;

                    lineTemplate.CoeffType = GetCoeffType(oddSet.bettype);

                    LineDTO line;

                    switch (oddSet.bettype)
                    {
                        #region Handicap

                        //HANDICAP
                        case 1:
                        //1H HANDICAP
                        case 7:
                        //2H Handicap //TODO:проверить параметры
                        case 17:
                        case 183:

                            line = lineTemplate.Clone();

                            var point = oddSet.hdp1 != 0m ? oddSet.hdp1 : -1m * oddSet.hdp2;

                            line.CoeffKind = "HANDICAP1";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.odds1a);
                            line.CoeffParam = -1 * point;
                            line.LineObject = $"{oddSet.oddsid}|h|{oddSet.bettype}|{oddSet.odds1a}";

                            AddLine(line.Clone());


                            line = lineTemplate.Clone();

                            line.CoeffKind = "HANDICAP2";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.odds2a);
                            line.CoeffParam = point;
                            line.LineObject = $"{oddSet.oddsid}|a|{oddSet.bettype}|{oddSet.odds2a}";

                            AddLine(line.Clone());

                            continue;
                      
                            

                        #endregion

                        //TODO
                        #region Individual Over/Under

                        //Home Team Over/Under
                        case 401:
                        case 461:
                        //Away Team Over/Under
                        case 402:
                        case 462:
                        //1H Home Team Over/Under
                        case 403:
                        case 463:
                        //1H Away Team Over/Under
                        case 464:

                            //TODO

                            continue;

                        #endregion

                        #region Over/Under

                        //FT OVER/UNDER
                        case 3:
                        //1H OVER/UNDER
                        case 8:

                            line = lineTemplate.Clone();

                            line.CoeffKind = "TOTALOVER";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.odds1a);
                            line.CoeffParam = oddSet.hdp1;
                            line.LineObject = $"{oddSet.oddsid}|h|{oddSet.bettype}|{oddSet.odds1a}";

                            AddLine(line.Clone());


                            line = lineTemplate.Clone();

                            line.CoeffKind = "TOTALUNDER";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.odds2a);
                            line.CoeffParam = oddSet.hdp1;
                            line.LineObject = $"{oddSet.oddsid}|a|{oddSet.bettype}|{oddSet.odds2a}";

                            AddLine(line.Clone());

                            continue;
                        //2H OVER/UNDER
                        case 18:
                        case 178:

                            line = lineTemplate.Clone();

                            line.CoeffKind = "TOTALOVER";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.odds1a);
                            line.CoeffParam = oddSet.hdp1;
                            line.LineObject = $"{oddSet.oddsid}|o|{oddSet.bettype}|{oddSet.odds1a}";

                            AddLine(line.Clone());


                            line = lineTemplate.Clone();

                            line.CoeffKind = "TOTALUNDER";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.odds2a);
                            line.CoeffParam = oddSet.hdp1;
                            line.LineObject = $"{oddSet.oddsid}|u|{oddSet.bettype}|{oddSet.odds2a}";

                            AddLine(line.Clone());

                            continue;

                        #endregion

                        #region 1X2

                        //1X2
                        case 5:
                        //1H 1X2
                        case 15:
                        //2H 1X2
                        case 430:
                        case 177:
                            line = lineTemplate.Clone();

                            line.CoeffKind = "1";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.com1);
                            line.LineObject = $"{oddSet.oddsid}|1|{oddSet.bettype}|{oddSet.com1}";

                            AddLine(line.Clone());


                            line = lineTemplate.Clone();

                            line.CoeffKind = "X";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.comx);
                            line.LineObject = $"{oddSet.oddsid}|x|{oddSet.bettype}|{oddSet.comx}";

                            AddLine(line.Clone());


                            line = lineTemplate.Clone();

                            line.CoeffKind = "2";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.com2);
                            line.LineObject = $"{oddSet.oddsid}|2|{oddSet.bettype}|{oddSet.com2}";

                            AddLine(line.Clone());

                            continue;

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

                            line = lineTemplate.Clone();

                            line.CoeffKind = "W1";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.odds1a);
                            line.LineObject = $"{oddSet.oddsid}|h|{oddSet.bettype}|{oddSet.odds1a}";

                            line.UpdateName();

                            AddLine(line.Clone());

                            line = lineTemplate.Clone();

                            line.CoeffKind = "W2";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.odds2a);
                            line.LineObject = $"{oddSet.oddsid}|a|{oddSet.bettype}|{oddSet.odds2a}";

                            line.UpdateName();

                            AddLine(line.Clone());

                            continue;

                        #endregion

                        #region Double chance

                        //Double chance
                        case 24:

                            line = lineTemplate.Clone();

                            line.CoeffKind = "1X";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.com1);
                            line.LineObject = $"{oddSet.oddsid}|1x|{oddSet.bettype}|{oddSet.com1}";

                            line.UpdateName();

                            AddLine(line.Clone());

                            line = lineTemplate.Clone();

                            line.CoeffKind = "12";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.comx);
                            line.LineObject = $"{oddSet.oddsid}|12|{oddSet.bettype}|{oddSet.comx}";

                            line.UpdateName();

                            AddLine(line.Clone());

                            line = lineTemplate.Clone();

                            line.CoeffKind = "X2";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.com2);
                            line.LineObject = $"{oddSet.oddsid}|2x|{oddSet.bettype}|{oddSet.com2}";

                            AddLine(line.Clone());

                            continue;
                        //1H Double chance
                        case 410:

                            line = lineTemplate.Clone();

                            line.CoeffKind = "1X";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.cs10);
                            line.LineObject = $"{oddSet.oddsid}|1x|{oddSet.bettype}|{oddSet.cs10}";

                            AddLine(line.Clone());


                            line = lineTemplate.Clone();

                            line.CoeffKind = "X2";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.cs20);
                            line.LineObject = $"{oddSet.oddsid}|2x|{oddSet.bettype}|{oddSet.cs20}";

                            AddLine(line.Clone());


                            line = lineTemplate.Clone();

                            line.CoeffKind = "12";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.cs00);
                            line.LineObject = $"{oddSet.oddsid}|12|{oddSet.bettype}|{oddSet.cs00}";

                            AddLine(line.Clone());

                            continue;
                        //2H Double chance
                        case 186:
                        case 431:

                            line = lineTemplate.Clone();

                            line.CoeffKind = "1X";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.cs10);
                            line.LineObject = $"{oddSet.oddsid}|hd|{oddSet.bettype}|{oddSet.cs10}";

                            AddLine(line.Clone());

                            line = lineTemplate.Clone();

                            line.CoeffKind = "X2";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.cs00);
                            line.LineObject = $"{oddSet.oddsid}|da|{oddSet.bettype}|{oddSet.cs00}";

                            AddLine(line.Clone());

                            line = lineTemplate.Clone();

                            line.CoeffKind = "12";
                            line.CoeffValue = ConvertToDecimalOdds(oddSet.cs20);
                            line.LineObject = $"{oddSet.oddsid}|ha|{oddSet.bettype}|{oddSet.cs20}";

                            AddLine(line.Clone());

                            continue;

                        #endregion
                    }

                }
            }


            return _lines.ToArray();
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

        private void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            _lines.Add(lineDto);
        }

    }
}


