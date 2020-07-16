using System;
using System.Collections.Generic;
using ServarrAPI.Datastore;

namespace ServarrAPI.Model
{
    public class UpdateEntity : ModelBase
    {
        public string Version { get; set; }
        public DateTime ReleaseDate { get; set; }
        public List<string> New { get; set; } = new List<string>();
        public List<string> Fixed { get; set; } = new List<string>();
        public string Branch { get; set; }

        public LazyLoaded<List<UpdateFileEntity>> UpdateFiles { get; set; } = new List<UpdateFileEntity>();
    }
}
