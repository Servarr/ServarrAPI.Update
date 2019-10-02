using System.Text.Json.Serialization;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureChange
    {

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

    }
}
