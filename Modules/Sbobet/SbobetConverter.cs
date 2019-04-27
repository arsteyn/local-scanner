using System;
using System.Collections.Generic;
using Bars.EAS.Utils.Extension;
using BM;
using BM.Core;
using BM.DTO;
using Sbobet.Models;

namespace Sbobet
{
    public class SbobetConverter
    {
        private List<LineDTO> _lines;

        public LineDTO[] Convert(MatchDataResult data, string bookmakerName)
        {
            _lines = new List<LineDTO>();

            return _lines.ToArray();
        }

       
        private void AddLine(LineDTO lineDto)
        {
            lineDto.UpdateName();
            _lines.Add(lineDto);
        }

    }
}


