using Newtonsoft.Json;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureArtifact
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("resource", Required = Required.Always)]
        public AzureResource Resource { get; set; }

    }
}
