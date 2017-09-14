namespace BetFairApi
{
    using Newtonsoft.Json;

    public class DeveloperAppVersion
    {
        [JsonProperty(PropertyName = "appVersions")]
        public string AppVersions { get; set; }

        [JsonProperty(PropertyName = "versionId")]
        public int VersionId { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "applicationKey")]
        public string ApplicationKey { get; set; }

        [JsonProperty(PropertyName = "delayData")]
        public bool DelayData { get; set; }

        [JsonProperty(PropertyName = "subscriptionRequired")]
        public bool SubscriptionRequired { get; set; }

        [JsonProperty(PropertyName = "ownerManaged")]
        public bool OwnerManaged { get; set; }

        [JsonProperty(PropertyName = "active")]
        public bool Active { get; set; }
    }
}
