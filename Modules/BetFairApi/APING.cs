using BM.Web;

namespace BetFairApi
{
    using Web;

    public abstract class APING
    {
        public const string APPKEY_HEADER = "X-Application";
        public const string SESSION_TOKEN_HEADER = "X-Authentication";

        public APING(string appKey)
        {
            this.ApiKey = appKey;
        }

        public virtual string ApiKey { get; set; }

        public virtual PostWebClient GetBetWebClient(string appKey, string token = null)
        {
            var wc = new PostWebClient();
         
            appKey.IfNotNull(x => wc.Headers[APPKEY_HEADER] = x);
            token.IfNotNull(x => wc.Headers[SESSION_TOKEN_HEADER] = x);

            wc.BeforeRequest = x => x.ContentType = "application/json";

          
            return wc;
        }
    }
}
