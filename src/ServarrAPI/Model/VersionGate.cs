using System;

namespace ServarrAPI.Model
{
    public class VersionGate
    {
        public Version MaxVersion { get; set; }
        public Version MaxUpgradeVersion { get; set; }
    }
}
