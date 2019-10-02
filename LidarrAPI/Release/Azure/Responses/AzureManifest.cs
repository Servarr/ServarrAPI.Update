using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureManifest
    {

        [JsonPropertyName("items")]
        public List<AzureFile> Files { get; set; }

    }

    public class AzureFile
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("blob")]
        public AzureBlob Blob { get; set; }
    }

    public class AzureBlob
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
