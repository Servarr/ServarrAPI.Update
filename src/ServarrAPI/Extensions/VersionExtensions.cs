using System;

namespace ServarrAPI.Extensions
{
    public static class VersionExtensions
    {
        public static long ToIntVersion(this Version version)
        {
            return (version.Major * 10000000000) + (version.Minor * 100000000) + (version.Build * 1000000) + version.Revision;
        }
    }
}
