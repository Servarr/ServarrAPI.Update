using Newtonsoft.Json;

namespace RadarrAPI.Release.AppVeyor.Responses
{
    public class AppVeyorJob
    {

        [JsonProperty("jobId", Required = Required.Always)]
        public string JobId { get; set; }

        [JsonProperty("artifactsCount", Required = Required.Always)]
        public int ArtifactsCount { get; set; }

        [JsonProperty("failedTestsCount", Required = Required.Always)]
        public int FailedTestsCount { get; set; }

        [JsonProperty("status", Required = Required.Always)]
        public string Status { get; set; }

    }
}
