using System.Net;
using BM.Web;

namespace BetFairApi
{
    public abstract class APING
    {
        public const string APPKEY_HEADER = "X-Application";
        public const string SESSION_TOKEN_HEADER = "X-Authentication";

        public APING(string appKey, WebProxy proxy)
        {
            this.ApiKey = appKey;
            Proxy = proxy;
        }

        public readonly WebProxy Proxy;

        public virtual string ApiKey { get; set; }

        public virtual PostWebClient PostBetWebClient(string appKey, string token = null)
        {
            var wc = new PostWebClient(Proxy);

            appKey.IfNotNull(x => wc.Headers[APPKEY_HEADER] = x);
            token.IfNotNull(x => wc.Headers[SESSION_TOKEN_HEADER] = x);

            wc.BeforeRequest = x => x.Accept = "application/json";
            wc.BeforeRequest = x => x.ContentType = "application/x-www-form-urlencoded";
            //wc.BeforeRequest = x => x.Timeout = 5000;
            //wc.BeforeRequest = x => x.KeepAlive = true; 

            return wc;
        }


    }
}
