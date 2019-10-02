using System.Threading.Tasks;
using LidarrAPI.Release;
using LidarrAPI.Update;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LidarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class WebhookController
    {
        private readonly Config _config;
        private readonly ReleaseService _releaseService;

        public WebhookController(ReleaseService releaseService, IOptions<Config> optionsConfig)
        {
            _releaseService = releaseService;
            _config = optionsConfig.Value;
        }

        [Route("refresh")]
        [HttpGet]
        [HttpPost]
        public async Task<string> Refresh([FromQuery] Branch branch, [FromQuery(Name = "api_key")] string apiKey)
        {
            if (!_config.ApiKey.Equals(apiKey))
            {
                return "No, thank you.";
            }

            await _releaseService.UpdateReleasesAsync(branch);

            return "Thank you.";
        }
    }
}