using System.Text.Json.Serialization;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureResource
    {

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }

    }
}
