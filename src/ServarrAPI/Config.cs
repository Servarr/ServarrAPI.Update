using System.Collections.Generic;
using ServarrAPI.Model;

namespace ServarrAPI
{
    public class Config
    {
        public Config()
        {
            VersionGates = new List<VersionGate>();
        }

        public string DataDirectory { get; set; }
        public string ApiKey { get; set; }
        public string Project { get; set; }
        public bool LogSql { get; set; }
        public List<VersionGate> VersionGates { get; set; }
    }
}
