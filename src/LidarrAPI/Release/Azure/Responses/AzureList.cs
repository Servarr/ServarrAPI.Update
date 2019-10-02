using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LidarrAPI.Release.Azure.Responses
{
    public class AzureList<T> where T : class
    {

        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("value")]
        public List<T> Value { get; set; }

    }
}
