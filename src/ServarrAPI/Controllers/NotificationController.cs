using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServarrAPI.Model;

namespace ServarrAPI.Controllers
{
    [Route("[controller]")]
    public class NotificationController : Controller
    {
        private readonly Config _config;
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService,
                                      IOptions<Config> optionsConfig)
        {
            _notificationService = notificationService;
            _config = optionsConfig.Value;
        }

        [HttpGet]
        public async Task<object> GetNotifications([FromQuery(Name = "branch")] string updateBranch,
                                                   [FromQuery(Name = "version")] string urlVersion,
                                                   [FromQuery(Name = "os")] OperatingSystem operatingSystem,
                                                   [FromQuery(Name = "runtimeVer")] string urlRuntimeVersion,
                                                   [FromQuery(Name = "runtime")] Runtime runtime = Runtime.DotNet,
                                                   [FromQuery(Name = "arch")] Architecture arch = Architecture.X64)
        {
            return await _notificationService.Find(urlVersion, updateBranch, operatingSystem, runtime, arch);
        }

        [HttpPost]
        public async Task<object> AddNotification([FromBody] Notification notification, [FromQuery(Name = "api_key")] string apiKey)
        {
            if (!_config.ApiKey.Equals(apiKey))
            {
                return new UnauthorizedResult();
            }

            try
            {
                return await _notificationService.Insert(notification);
            }
            catch
            {
                return new BadRequestResult();
            }
        }

        [Route("{id}")]
        [HttpDelete]
        public object DeleteNotification([FromRoute(Name = "id")] int id, [FromQuery(Name = "api_key")] string apiKey)
        {
            if (!_config.ApiKey.Equals(apiKey))
            {
                return new UnauthorizedResult();
            }

            _notificationService.Delete(id);

            return new OkResult();
        }
    }
}
