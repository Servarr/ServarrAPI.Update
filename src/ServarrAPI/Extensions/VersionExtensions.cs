using System;

namespace ServarrAPI.Extensions
{
    public static class VersionExtensions
    {
        public static long ToIntVersion(this Version version)
        {
            return (version.Major * 10000000000L) + (version.Minor * 100000000L) + (version.Build * 1000000L) + version.Revision;
        }
    }
}
