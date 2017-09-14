namespace BetFairApi.Web
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JsonResponse<T>
    {
        [JsonProperty(PropertyName = "jsonrpc", NullValueHandling = NullValueHandling.Ignore)]
        public string JsonRpc { get; set; }

        [JsonProperty(PropertyName = "result", NullValueHandling = NullValueHandling.Ignore)]
        public T Result { get; set; }

        [JsonProperty(PropertyName = "error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonResponse<T>.Exception Error { get; set; }

        [JsonProperty(PropertyName = "id")]
        public object Id { get; set; }

        [JsonIgnore]
        public bool HasError
        {
            get { return Error != null; }
        }

        public class Exception
        {
            // exception in json-rpc format
            [JsonProperty(PropertyName = "data")]
            public JObject Data { get; set; }		// actual exception details


            // exception in rescript format
            [JsonProperty(PropertyName = "detail")]
            public JObject Detail { get; set; }		// actual exception details

        }
    }
}
