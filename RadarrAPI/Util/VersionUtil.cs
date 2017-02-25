using System;

namespace RadarrAPI.Util
{
    public static class VersionUtil
    {

        /// <summary>
        ///     Check if a version is a valid <see cref="Version"/>.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>Returns true if <see cref="version"/> is valid.</returns>
        public static bool IsValid(string version)
        {
            Version v;
            return Version.TryParse(version, out v);
        }

    }
}
