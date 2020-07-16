using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace ServarrAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class VersionController : ControllerBase
    {
        [Route("")]
        public object Version()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var version = assembly.GetName().Version.ToString();

            var attributes = assembly.GetCustomAttributes(true);

            var config = attributes.OfType<AssemblyConfigurationAttribute>().FirstOrDefault();
            var branch = config?.Configuration ?? "unknown";

            return new
            {
                Version = version,
                Branch = branch
            };
        }
    }
}
