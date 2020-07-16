using System.Collections.Generic;

namespace ServarrAPI.Controllers.Update
{
    public class UpdateChanges
    {
        public List<string> New { get; set; } = new List<string>();

        public List<string> Fixed { get; set; } = new List<string>();
    }
}
