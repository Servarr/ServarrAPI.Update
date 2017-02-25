using Newtonsoft.Json;

namespace RadarrAPI.Release.AppVeyor.Responses
{
    public class AppVeyorProjectLastBuild
    {
        
        [JsonProperty("build")]
        public AppVeyorProjectBuild Build { get; set; }

    }
}
