using System.Collections.Generic;
using ServarrAPI.Datastore;

namespace ServarrAPI.Model
{
    public class Notification : ModelBase
    {
        public Notification()
        {
            OperatingSystems = new List<OperatingSystem>();
            Runtimes = new List<Runtime>();
            Architectures = new List<System.Runtime.InteropServices.Architecture>();
            Versions = new List<string>();
            Branches = new List<string>();
        }

        public NotificationType Type { get; set; }
        public string Message { get; set; }
        public string WikiUrl { get; set; }
        public List<OperatingSystem> OperatingSystems { get; set; }
        public List<Runtime> Runtimes { get; set; }
        public List<System.Runtime.InteropServices.Architecture> Architectures { get; set; }
        public List<string> Versions { get; set; }
        public List<string> Branches { get; set; }
    }

    public enum NotificationType
    {
        Notice = 1,
        Warning = 2,
        Error = 3
    }
}
