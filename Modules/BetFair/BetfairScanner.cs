using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BetFair.Config;
using BetFair.Enums;
using BM.Core;
using BM.DTO;
using Exception = System.Exception;
using BetFairApi;
using BetFairApi.Web;
using Scanner;

namespace BetFair
{
    public class BetfairScanner : ScannerBase
    {
        public override string Name => "Betfair";

        public override string Host => AccountAPING.url;

        private CachedArray<CookieCollection> _cachedArray;

        protected override void UpdateLiveLines()
        {
            var lines = new List<LineDTO>();

            var converter = new BetFairLineConverter();

            try
            {
                var cookieCollection = _cachedArray.GetData();

                var token = BetFairHelper.GetAuthField(cookieCollection, AuthField.Token);

                var appKey = BetFairHelper.GetAuthField(cookieCollection, AuthField.Developer);

                try
                {
                    var r = ProxyList.PickRandom();
                    var webProxy = new WebProxy(r.Address)
                    {
                        Credentials = new NetworkCredential(r.Credentials.GetCredential(r.Address, "").UserName, r.Credentials.GetCredential(r.Address, "").Password)
                    };

                    lines = converter.Convert($"{token}|{appKey}", Name, webProxy).ToList();
                }
                catch (Exception e)
                {
                    Log.Info("BF Parse event exception " + e.InnerException);
                }

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count, new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                ActualLines = lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info("ERROR BF " + e.Message + e.StackTrace);
                Console.WriteLine("ERROR BF " + e.Message + e.StackTrace);
            }
        }


        protected override void CheckDict()
        {
            var hostsToDelete = new List<WebProxy>();

            Parallel.ForEach(ProxyList, (host, state) =>
            {
                try
                {
                    var aping = new AccountAPING(BetFairData.ApiKey, host);

                    //webhost
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "LiveApp.p12");

                    Log.Info("path " + path);

                    var file = new MemoryStream(File.ReadAllBytes(path));

                    var authParams = new AuthParams
                    {
                        Login = BetFairData.Login,
                        Password = BetFairData.Password,
                        UseCertificate = true,
                        Certificate = new X509Certificate2(file.ToArray(), BetFairData.CertificatePassword, X509KeyStorageFlags.MachineKeySet)
                    };

                    aping.GetSession(authParams);
                }
                catch (Exception e)
                {
                    hostsToDelete.Add(host);
                }
            });

            foreach (var host in hostsToDelete) ProxyList.Remove(host);

            _cachedArray = new CachedArray<CookieCollection>(1000 * 60 * 60, () => Authorize.DoAuthorize(ProxyList.First()));
        }
    }
}
