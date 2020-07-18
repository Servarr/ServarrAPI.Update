using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServarrAPI.Cloudflare;

namespace ServarrAPI.Release
{
    public class ReleaseService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICloudflareProxy _cloudflare;

        public ReleaseService(IServiceProvider serviceProvider,
                              ICloudflareProxy cloudflare)
        {
            _serviceProvider = serviceProvider;
            _cloudflare = cloudflare;
        }

        public async Task UpdateReleasesAsync(Type releaseSource)
        {
            var releaseSourceInstance = (ReleaseSourceBase)_serviceProvider.GetRequiredService(releaseSource);

            var updatedBranches = await releaseSourceInstance.StartFetchReleasesAsync().ConfigureAwait(false);
            await _cloudflare.InvalidateBranches(updatedBranches).ConfigureAwait(false);
        }
    }
}
