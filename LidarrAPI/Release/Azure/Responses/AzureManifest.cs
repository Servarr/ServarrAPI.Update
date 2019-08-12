using System.Collections.Generic;
using Newtonsoft.Json;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureManifest
    {

        [JsonProperty("items", Required = Required.Always)]
        public List<AzureFile> Files { get; set; }

    }

    public class AzureFile
    {
        [JsonProperty("path", Required = Required.Always)]
        public string Path { get; set; }

        [JsonProperty("blob", Required = Required.Always)]
        public AzureBlob Blob { get; set; }
    }

    public class AzureBlob
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }
    }
}
