using System.Runtime.Serialization;

namespace BetFairApi
{
    using Newtonsoft.Json;


    [DataContract]
    public class LoginResponse
    {
        [DataMember(Name = "sessionToken")]
        public string SessionToken { get; set; }
        [DataMember(Name = "loginStatus")]
        public string LoginStatus { get; set; }
    }

    class LoginResult
    {
        [JsonProperty(PropertyName = "sessionToken")]
        public string SessionToken { get; set; }

        [JsonProperty(PropertyName = "loginStatus")]
        public string LoginStatus { get; set; }
    }
}
