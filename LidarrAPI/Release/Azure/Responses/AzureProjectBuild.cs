using System;
using Newtonsoft.Json;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureProjectBuild
    {

        [JsonProperty("id")]
        public int BuildId { get; set; }

        [JsonProperty("buildNumber", Required = Required.Always)]
        public string Version { get; set; }

        [JsonProperty("status", Required = Required.Always)]
        public string Status { get; set; }

        [JsonProperty("result", Required = Required.Always)]
        public string Result { get; set; }

        [JsonProperty("startTime")]
        public DateTimeOffset? Started { get; set; }

    }
}
