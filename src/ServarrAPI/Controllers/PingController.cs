using Microsoft.AspNetCore.Mvc;

namespace ServarrAPI.Controllers
{
    [Route("[controller]")]
    public class PingController
    {
        [HttpGet]
        public string Ping()
        {
            return "Pong";
        }
    }
}
