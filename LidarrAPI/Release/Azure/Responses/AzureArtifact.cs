using System.Text.Json.Serialization;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureArtifact
    {

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resource")]
        public AzureResource Resource { get; set; }

    }
}
