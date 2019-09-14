using System.Text.RegularExpressions;

namespace LidarrAPI.Util
{
    /// <summary>
    ///     This class is used to define compiled regexes
    ///     that are used to parse information from GitHub releases.
    /// </summary>
    public static class RegexUtil
    {
        public static readonly Regex ReleaseFeaturesGroup = new Regex(@"\*\s+[0-9a-f]{40}\s+New:\s*(?<text>.*?)\r*$", RegexOptions.Compiled | RegexOptions.Multiline);
        
        public static readonly Regex ReleaseFixesGroup = new Regex(@"\*\s+[0-9a-f]{40}\s+Fixed:\s*(?<text>.*?)\r*$", RegexOptions.Compiled | RegexOptions.Multiline);
    }
}
