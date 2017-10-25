using System.Text.RegularExpressions;

namespace RadarrAPI.Util
{
    /// <summary>
    ///     This class is used to define compiled regexes
    ///     that are used to parse information from GitHub releases.
    /// </summary>
    public static class RegexUtil
    {
        public static readonly Regex ReleaseFeaturesGroup = new Regex(@"\*\*New features:\*\*\r\n(?<features>.*?\r\n)\r\n", RegexOptions.Compiled | RegexOptions.Singleline);
        
        public static readonly Regex ReleaseFixesGroup = new Regex(@"\*\*Fixes:\*\*\r\n(?<fixes>.*?\r\n)\r\n", RegexOptions.Compiled | RegexOptions.Singleline);
        
        public static readonly Regex ReleaseChange = new Regex(@"- (?<text>.*?)\r\n", RegexOptions.Compiled);
    }
}
