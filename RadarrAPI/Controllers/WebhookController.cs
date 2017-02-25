using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RadarrAPI.Release;
using RadarrAPI.Update;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class WebhookController
    {
        private readonly ReleaseService _releaseService;
        private readonly Config _config;

        public WebhookController(ReleaseService releaseService, IOptions<Config> optionsConfig)
        {
            _releaseService = releaseService;
            _config = optionsConfig.Value;
        }

        [Route("refresh")]
        [HttpGet, HttpPost]
        public string Refresh([FromQuery] Branch branch, [FromQuery(Name = "api_key")] string apiKey)
        {
            if (!_config.ApiKey.Equals(apiKey))
            {
                return "No, thank you.";
            }

            _releaseService.UpdateReleases(branch);

            return "Thank you.";
        }
    }
}
