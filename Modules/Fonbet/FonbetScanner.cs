using System;
using System.Diagnostics;
using System.Linq;
using BM.Core;
using BM.Web;
using FonBet.SerializedClasses;
using Scanner;
using Utf8Json;

namespace Fonbet
{
    public class FonbetScanner : ScannerBase
    {
        internal UrlsJson Urls;

        public override string Name => "Fonbet";

        public override string Host => "https://www.bkfonbet.com/";


        protected override void UpdateLiveLines()
        {
            try
            {
                var st = new Stopwatch();

                st.Start();

                var url = $"https:{Urls.line.PickRandom()}/live/currentLine/en/?{Helper.GetJavascriptRandomValue()}";

                LiveData data;

                using (var wc = new GetWebClient(ProxyList.PickRandom()))
                {
                    //data = wc.DownloadResult<LiveData>(url);

                    var d = wc.DownloadString(url);

                    data = JsonSerializer.Deserialize<LiveData>(d);

                }

                var converter = new FonbetLineConverter();

                var lines = converter.Convert(data, Name);

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count(c => c != null), new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                ActualLines = lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info($"ERROR {Name} {e.Message} {e.StackTrace}");
            }
        }

        protected override void CheckDict()
        {
            base.CheckDict();

            Urls = UpdateUrls();
        }

        public UrlsJson UpdateUrls()
        {
            using (var wc = new GetWebClient(ProxyList.PickRandom()))
                return wc.DownloadResult<UrlsJson>($"{Host}urls.json");
        }
    }
}
