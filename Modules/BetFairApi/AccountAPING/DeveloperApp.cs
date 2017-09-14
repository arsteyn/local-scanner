namespace BetFairApi
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class DeveloperApp
    {
        [JsonProperty(PropertyName = "appName")]
        public string AppName { get; set; }

        [JsonProperty(PropertyName = "appId")]
        public string AppId { get; set; }

        [JsonProperty(PropertyName = "appVersions")]
        public List<DeveloperAppVersion> AppVersions { get; set; }
    }
}
