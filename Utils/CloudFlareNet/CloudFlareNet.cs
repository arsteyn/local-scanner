using Extreme.Net;
using System.IO;
using System.Threading;

namespace CloudFlareNet
{
    public class CloudFlareNet
    {
        private const string CloudFlareServerName = "cloudflare-nginx";
        private const string IdCookieName         = "__cfduid";
        private const string ClearanceCookieName  = "cf_clearance";


        public static CookieDictionary GetCloudflareCookies(string uri, string userAgent, ProxyClient proxy = null)
        {
            var request = new HttpRequest();
            request.IgnoreProtocolErrors = true;
            request.AllowAutoRedirect = false;
            request.UserAgent = userAgent;
            request.Referer = uri;
            request.Proxy = proxy;

            var response = request.Get(uri);

            if (!IsClearanceRequired(response)) return request.Cookies;

            do
            {
                response = request.Get(uri);

                string responseString = new StreamReader(response.ToMemoryStream()).ReadToEnd();

                PassClearance(request, response, responseString);

            } while (IsClearanceRequired(response));

            return request.Cookies;
        }

        private static void PassClearance(HttpRequest request, HttpResponse response, string content)
        {
            var pageContent = content;
            var scheme = response.Address.Scheme;
            var host = response.Address.Host;
            var solution = ChallengeSolver.Solve(pageContent, host);
            var clearanceUri = $"{scheme}://{host}{solution.ClearanceQuery}";

            //await Task.Delay(5000);
            Thread.Sleep(5000);

            request.Cookies = response.Cookies;

            var resp = request.Get(clearanceUri);

            request.Cookies = resp.Cookies;
        }

        private static bool IsClearanceRequired(HttpResponse response)
        {
            var isServiceUnavailable = response.StatusCode == HttpStatusCode.ServiceUnavailable;
            var isCloudFlareServer = false;

            var headers = response.EnumerateHeaders();

            while (headers.MoveNext())
            {
                var element = headers.Current;

                if (element.Key != "Server") continue;

                if (element.Value != CloudFlareServerName) continue;

                isCloudFlareServer = true;
            }

            return isServiceUnavailable && isCloudFlareServer;
        }
    }
}
