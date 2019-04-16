using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BetFair.Enums;
using BM.Core;
using BM.DTO;
using Exception = System.Exception;
using BetFairApi;
using Scanner;

namespace BetFair
{
    public class BetfairScanner : ScannerBase
    {
        public override string Name => "Betfair";

        public override string Host => AccountAPING.url;

        static CachedArray<CookieCollection> _cachedArray;
        static CachedArray<CookieCollection> CachedArray => _cachedArray ?? (_cachedArray = new CachedArray<CookieCollection>(1000 * 60 * 60, Authorize.DoAuthorize));

        protected override void UpdateLiveLines()
        {
            var lines = new List<LineDTO>();

            var converter = new BetFairLineConverter();

            try
            {
                var cookieCollection = CachedArray.GetData();

                var token = BetFairHelper.GetAuthField(cookieCollection, AuthField.Token);

                var appKey = BetFairHelper.GetAuthField(cookieCollection, AuthField.Developer);

                try
                {
                    lines = converter.Convert($"{token}|{appKey}", Name).ToList();
                }
                catch (Exception e)
                {
                    Log.Info("BF Parse event exception " + e.InnerException);
                }

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                ActualLines = lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info("ERROR BF " + e.Message + e.StackTrace);
                Console.WriteLine("ERROR BF " + e.Message + e.StackTrace);
            }
        }
    }
}
