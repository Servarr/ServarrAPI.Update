using System;
using System.Text.Json.Serialization;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureProjectBuild
    {

        [JsonPropertyName("id")]
        public int BuildId { get; set; }

        [JsonPropertyName("buildNumber")]
        public string Version { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("result")]
        public string Result { get; set; }

        [JsonPropertyName("startTime")]
        public DateTimeOffset? Started { get; set; }

    }
}
