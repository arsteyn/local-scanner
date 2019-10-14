using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using BetFair.Config;
using BetFair.Enums;
using BetFairApi;
using BetFairApi.Web;
using NLog;

namespace BetFair
{
    public class Authorize
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static CookieCollection DoAuthorize()
        {
            var cookieCollection = new CookieCollection();

            var aping = new AccountAPING(BetFairData.ApiKey);

            //var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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

            var token = aping.GetSession(authParams);
            var appKeys = aping.GetDeveloperAppKeys(token);
            var app = appKeys.First();

            cookieCollection.Add(new Cookie(AuthField.Token.ToString(), token, "/", "BetFair.com"));
            cookieCollection.Add(new Cookie(AuthField.ApiKey.ToString(), BetFairData.ApiKey, "/", "BetFair.com"));
            cookieCollection.Add(new Cookie(AuthField.Developer.ToString(), app.AppVersions[0].ApplicationKey, "/", "BetFair.com"));
            cookieCollection.Add(new Cookie(AuthField.Customer.ToString(), app.AppVersions[1].ApplicationKey, "/", "BetFair.com"));

            return cookieCollection;
        }
    }
}
