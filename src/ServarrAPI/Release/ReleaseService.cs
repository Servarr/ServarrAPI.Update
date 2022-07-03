using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServarrAPI.Cloudflare;

namespace ServarrAPI.Release
{
    public class ReleaseService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICloudflareProxy _cloudflare;
        private readonly HttpClient _httpClient;
        private readonly Config _config;

        public ReleaseService(IServiceProvider serviceProvider,
                              IOptions<Config> configOptions,
                              ICloudflareProxy cloudflare)
        {
            _serviceProvider = serviceProvider;
            _cloudflare = cloudflare;

            _httpClient = new HttpClient();

            _config = configOptions.Value;
        }

        public async Task UpdateReleasesAsync(Type releaseSource)
        {
            var releaseSourceInstance = (ReleaseSourceBase)_serviceProvider.GetRequiredService(releaseSource);

            var updatedBranches = await releaseSourceInstance.StartFetchReleasesAsync().ConfigureAwait(false);

            if (updatedBranches != null && updatedBranches.Count > 0)
            {
                foreach (var branch in updatedBranches)
                {
                    await CallTriggers(branch);
                }
            }

            await _cloudflare.InvalidateBranches(updatedBranches).ConfigureAwait(false);
        }

        private async Task CallTriggers(string branch)
        {
            var triggers = _config.Triggers;

            foreach (var trigger in triggers)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, trigger.Url);
                    request.Headers.UserAgent.ParseAdd("RadarrAPI.Update/Trigger");
                    request.Headers.ConnectionClose = true;

                    if (!string.IsNullOrWhiteSpace(trigger.AuthToken))
                    {
                        request.Headers.Add("Authorization", "Bearer " + trigger.AuthToken);
                    }

                    string json = JsonConvert.SerializeObject(new { Application = _config.Project, Branch = branch });
                    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                    request.Content = httpContent;

                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(2500);

                    var response = await _httpClient.SendAsync(request, cts.Token);
                    response.Dispose();
                }
                catch (Exception)
                {
                    // don't care.
                }
            }
        }
    }
}
