using System.Net;
using Newtonsoft.Json;

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

        public static readonly string GET_DEVELOPER_APP_KEYS = $"{service}/{version}/getDeveloperAppKeys";

        public static readonly string GET_ACCOUNT_FUNDS = $"{service}/{version}/getAccountFunds";



        public AccountAPING(string appKey, WebProxy proxy)
            : base(appKey, proxy)
        {
        }

        /// <summary>
        /// Получение сессии
        /// </summary>
        /// <param name="bmUserGetWebProxy"></param>
        /// <param name="authParams"></param>
        /// <returns>Session Token (ssoid)</returns>
        public string GetSession(AuthParams authParams)
        {
            using (var wc = this.PostBetWebClient(this.ApiKey))
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

                var r = wc.Post("https://identitysso-cert.betfair.com/api/certlogin", values);

                var request = JsonConvert.DeserializeObject<LoginResult>(r);

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
        /// <param name="bmUserGetWebProxy"></param>
        /// <param name="token">Session Token (ssoid)</param>
        /// <returns></returns>
        public List<DeveloperApp> GetDeveloperAppKeys(string token)
        {
            token.IfNull(x => throw new ArgumentException("token"));

            var param = new JsonRequest
            {
                Id = "1",
                Method = GET_DEVELOPER_APP_KEYS
            };

            using (var wc = this.PostBetWebClient(null, token))
            {
                var r = wc.Post(url, param);

                var response = JsonConvert.DeserializeObject<JsonResponse<List<DeveloperApp>>>(r);

                return response.Result;
            }
        }



        public AccountFundsResponse GetAccountFunds(string token, string appKey)
        {
            token.IfNull(x => throw new ArgumentException("token"));
            appKey.IfNull(x => throw new ArgumentException("appKey"));

            var param = new JsonRequest
            {
                Id = "1",
                Method = GET_ACCOUNT_FUNDS
            };

            using (var wc = PostBetWebClient(appKey, token))
            {
                var r = wc.Post(url, param);

                var response = JsonConvert.DeserializeObject<JsonResponse<AccountFundsResponse>>(r);

                return response.Result;
            }
        }
    }
}
