namespace BetFairApi
{
    using System;
    using System.Collections.Generic;
    using Web;
    using System.Collections.Specialized;

    public class AccountAPING : APING
    {
        public const string service = "AccountAPING";

        public const string version = "v1.1";

        public const string url = "https://api.betfair.com/exchange/account/json-rpc/v1/";

        public static readonly string GET_DEVELOPER_APP_KEYS = string.Format("{0}/{1}/getDeveloperAppKeys", service, version);

        public static readonly string GET_ACCOUNT_FUNDS = string.Format("{0}/{1}/getAccountFunds", service, version);

        public AccountAPING(string appKey)
            : base(appKey)
        {
        }

        /// <summary>
        /// Получение сессии
        /// </summary>
        /// <param name="authParams"></param>
        /// <returns>Session Token (ssoid)</returns>
        public string GetSession(AuthParams authParams)
        {
            using (var wc = this.GetBetWebClient(this.ApiKey))
            {
                if (authParams.UseCertificate && authParams.Certificate != null)
                {
                    wc.BeforeRequest = x => x.ClientCertificates.Add(authParams.Certificate);
                }

                var values = new NameValueCollection
                {
                    { "username", authParams.Login },
                    { "password", authParams.Password }
                };


                var request = wc.Post<LoginResult>("https://identitysso.betfair.com/api/certlogin", values);

                if (request.LoginStatus == "SUCCESS")
                {
                    return request.SessionToken;
                }

                throw new System.Exception(request.LoginStatus);
            }
        }

        /// <summary>
        /// Получить ключи приложений разработчика
        /// </summary>
        /// <param name="token">Session Token (ssoid)</param>
        /// <returns></returns>
        public List<DeveloperApp> GetDeveloperAppKeys(string token)
        {
            token.IfNull(x => { throw new ArgumentException("token"); });

            var param = new JsonRequest
            {
                Id = "1",
                Method = GET_DEVELOPER_APP_KEYS
            };

            using (var wc = this.GetBetWebClient(null, token))
            {
                var response = wc.Post<JsonResponse<List<DeveloperApp>>>(url, param);
                return response.Result;
            }
        }

        public AccountFundsResponse GetAccountFunds(string token, string appKey)
        {
            token.IfNull(x => { throw new ArgumentException("token"); });
            appKey.IfNull(x => { throw new ArgumentException("appKey"); });

            var param = new JsonRequest
            {
                Id = "1",
                Method = GET_ACCOUNT_FUNDS
            };

            using (var wc = this.GetBetWebClient(appKey, token))
            {
                var response = wc.Post<JsonResponse<AccountFundsResponse>>(url, param);
                return response.Result;
            }
        }
    }
}
