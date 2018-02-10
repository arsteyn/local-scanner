using System;
using System.Linq;
using System.Text;
using AngleSharp.Parser.Html;
using Bars.EAS.Utils;
using BM.Core;
using BM.DTO;
using Parimatch.AdditionalClasses;
using Scanner;

namespace Parimatch
{
    public class ParimatchScanner : ScannerBase
    {
        public override string Name => "Parimatch";
        //public override string Host => "https://www.parimatch.com/";
        public override string Host => "https://www.pm-101.info//";

        CachedArray<TempData> _cachedArray;
        CachedArray<TempData> CachedArray => _cachedArray ?? (_cachedArray = new CachedArray<TempData>(1000 * 60 * 20, UpdateUrls));

        private TempData UpdateUrls(object data)
        {
            Func<string, string> load = x =>
            {
                using (var wc = new Extensions.WebClientEx(ProxyList.PickRandom()))
                {
                    wc.Headers["Upgrade-Insecure-Requests"] = "1";
                    wc.Headers["Referer"] = $"{Host}/en/live.html";
                    return wc.DownloadString(x);
                }
            };

            var url = $"{Host}/en/live_as.html?curs=0&curName=$&shed=0";

            var html = load(url);

            var parser = new HtmlParser();
            var doc = parser.Parse(html);
            var inputs = doc.QuerySelectorAll("input.ch_l");

            var values = inputs.Select(x => x.GetAttribute("value").ToInt()).ToList();

            var hl = string.Join(",", values);
            var he = string.Join(",", values.OrderBy(x => x));

            var tempData = new TempData
            {
                Referer = $"{Host}/en/bet.html?ARDisabled=on&hl={hl}"
            };

            tempData.Html = load(tempData.Referer);
            tempData.NeedUpdate = false;
            tempData.Url = $"{Host}/en/live_ar.html?ARDisabled=on&hl={hl}&he={he}&curs=0&curName=$";

            return tempData;
        }
        protected override LineDTO[] GetLiveLines()
        {
            try
            {
                var tempData = CachedArray.GetData();

                if (tempData.NeedUpdate)
                {
                    using (var wc = new Extensions.WebClientEx(ProxyList.PickRandom()))
                    {
                        wc.Encoding = Encoding.Default;
                        wc.Headers["Referer"] = tempData.Referer;
                        tempData.Html = wc.DownloadString(tempData.Url);
                    }
                }
                else
                {
                    tempData.NeedUpdate = true;
                }

                var converter = new ParimatchLineConverter();


                var lines = converter.Convert(tempData, Name);

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Length, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                return lines;
            }
            catch
            {
                return new LineDTO[] { };
            }
        }

    }
}
