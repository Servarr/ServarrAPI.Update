using System.Net.Http;

namespace ServarrAPI.Model
{
    public class Trigger
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string AuthToken { get; set; }
    }
}
