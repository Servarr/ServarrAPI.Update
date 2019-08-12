using Newtonsoft.Json;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureChange
    {

        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("message", Required = Required.Always)]
        public string Message { get; set; }

    }
}
