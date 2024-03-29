using System;
using System.Text.RegularExpressions;
using Serilog;

namespace ServarrAPI.Util
{
    public class UserAgentInfo
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(UserAgentInfo));
        private static readonly Regex ParseRegex = new Regex(@"(?<app>Radarr|Prowlarr|Lidarr|Readarr|Sonarr|Whisparr)\/(?<version>\d.*)\W\((?<os>.+?)(?:\s(?<os_version>\d.*))?\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string App { get; }
        public string Name { get; }
        public string OsVersion { get; }
        public string Version { get; }

        public UserAgentInfo(string userAgent)
        {
            try
            {
                var parseResult = ParseRegex.Match(userAgent);

                if (parseResult.Success)
                {
                    App = parseResult.Groups["app"]?.Value.ToLower().Trim();
                    Name = parseResult.Groups["os"]?.Value.ToLower().Trim();
                    OsVersion = parseResult.Groups["os_version"]?.Value.ToLower().Trim();
                    Version = parseResult.Groups["version"]?.Value.ToLower().Trim();

                    if (string.IsNullOrWhiteSpace(OsVersion))
                    {
                        OsVersion = "Unknown";
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Couldn't parse user agent " + userAgent);
            }
        }
    }
}
