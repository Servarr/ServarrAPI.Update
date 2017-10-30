using System.Collections.Generic;
using Newtonsoft.Json;

namespace LidarrAPI.Release.AppVeyor.Responses
{
    public class AppVeyorProjectHistory
    {
        
        [JsonProperty("builds")]
        public List<AppVeyorProjectBuild> Builds { get; set; }

    }
}
