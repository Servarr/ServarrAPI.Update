using Newtonsoft.Json;

namespace RadarrAPI.Update.Data
{
    public class UpdatePackageContainer
    {

        /// <summary>
        ///     Set to true if this is an update the client needs.
        ///     If set to false, don't add a <see cref="UpdatePackage"/>.
        /// </summary>
        public bool Available { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public UpdatePackage UpdatePackage { get; set; }

    }
}
