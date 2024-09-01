using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServarrAPI.Cloudflare;
using ServarrAPI.Release;
using ServarrAPI.Release.Azure;
using ServarrAPI.Release.Github;
using ServarrAPI.TaskQueue;

namespace ServarrAPI.Controllers
{
    [Route("[controller]")]
    public class WebhookController
    {
        private readonly Config _config;
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ICloudflareProxy _cloudflare;

        public WebhookController(IBackgroundTaskQueue queue,
                                 IServiceScopeFactory serviceScopeFactory,
                                 ICloudflareProxy cloudflare,
                                 IOptions<Config> optionsConfig)
        {
            _queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
            _cloudflare = cloudflare;
            _config = optionsConfig.Value;
        }

        [Route("refresh")]
        [HttpGet]
        [HttpPost]
        public string Refresh([FromQuery] string source, [FromQuery(Name = "api_key")] string apiKey)
        {
            if (!_config.ApiKey.Equals(apiKey))
            {
                return "No, thank you.";
            }

            var type = source.ToLower() switch
            {
                "azure" => typeof(AzureReleaseSource),
                "github" => typeof(GithubReleaseSource),
                _ => null
            };

            if (type == null)
            {
                return $"Unknown source {source}";
            }

            _queue.QueueBackgroundWorkItem(async token =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var releaseService = scopedServices.GetRequiredService<ReleaseService>();

                await releaseService.UpdateReleasesAsync(type);
            });

            return "Thank you.";
        }

        [Route("branch/{branch}/refresh")]
        [HttpGet]
        public async Task<string> InvalidateCache([FromQuery(Name = "api_key")] string apiKey, [FromRoute(Name = "branch")] string branch)
        {
            if (!_config.ApiKey.Equals(apiKey))
            {
                return "No, thank you.";
            }

            if (string.IsNullOrWhiteSpace(branch))
            {
                return "Unknown branch.";
            }

            await _cloudflare.InvalidateBranches(new[] { branch }).ConfigureAwait(false);

            return "Thank you.";
        }
    }
}
