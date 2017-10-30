using Newtonsoft.Json;

namespace LidarrAPI.Release.AppVeyor.Responses
{
    public class AppVeyorProjectLastBuild
    {
        
        [JsonProperty("build")]
        public AppVeyorProjectBuild Build { get; set; }

    }
}
