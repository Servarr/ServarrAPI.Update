using System;

namespace ServarrAPI.Controllers.Update
{
    public class UpdatePackage
    {
        public string Version { get; set; }

        public DateTime ReleaseDate { get; set; }

        public string Filename { get; set; }

        public string Url { get; set; }

        public UpdateChanges Changes { get; set; }

        /// <summary>
        ///     Must be a SHA256 hash of the zip file obtained from <see cref="Url"/>.
        /// </summary>
        public string Hash { get; set; }

        public string Branch { get; set; }

        public string Runtime { get; set; }

        /// <summary>
        ///     The Status of the build, in theory should always be "success"
        /// </summary>
        public string Status { get; set; }
    }
}
