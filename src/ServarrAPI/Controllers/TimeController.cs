using System;
using Microsoft.AspNetCore.Mvc;

namespace ServarrAPI.Controllers
{
    [Route("[controller]")]
    public class TimeController
    {
        [HttpGet]
        public object Time()
        {
            var time = DateTime.UtcNow.ToString("o");

            return new
            {
                DateTimeUtc = time
            };
        }
    }
}
