using ServarrAPI.Datastore;

namespace ServarrAPI.Model
{
    public class UpdateFileEntity : ModelBase
    {
        public int UpdateId { get; set; }
        public OperatingSystem OperatingSystem { get; set; }
        public Runtime Runtime { get; set; }
        public System.Runtime.InteropServices.Architecture Architecture { get; set; }
        public string Filename { get; set; }
        public string Url { get; set; }
        public string Hash { get; set; }

        public LazyLoaded<UpdateEntity> Update { get; set; }
    }
}
