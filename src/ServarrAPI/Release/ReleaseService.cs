using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServarrAPI.Cloudflare;
using ServarrAPI.Release.Azure;
using ServarrAPI.Release.Github;

namespace ServarrAPI.Release
{
    public class ReleaseService
    {
        private static readonly ConcurrentDictionary<Type, SemaphoreSlim> ReleaseLocks;

        private readonly IServiceProvider _serviceProvider;
        private readonly ICloudflareProxy _cloudflare;

        static ReleaseService()
        {
            ReleaseLocks = new ConcurrentDictionary<Type, SemaphoreSlim>();
            ReleaseLocks.TryAdd(typeof(GithubReleaseSource), new SemaphoreSlim(1, 1));
            ReleaseLocks.TryAdd(typeof(AzureReleaseSource), new SemaphoreSlim(1, 1));
        }

        public ReleaseService(IServiceProvider serviceProvider,
                              ICloudflareProxy cloudflare)
        {
            _serviceProvider = serviceProvider;
            _cloudflare = cloudflare;
        }

        public async Task UpdateReleasesAsync(Type releaseSource)
        {
            if (!ReleaseLocks.TryGetValue(releaseSource, out var releaseLock))
            {
                throw new NotImplementedException($"{releaseSource} does not have a release lock.");
            }

            var obtainedLock = false;

            try
            {
                obtainedLock = await releaseLock.WaitAsync(TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (obtainedLock)
                {
                    var releaseSourceInstance = (ReleaseSourceBase)_serviceProvider.GetRequiredService(releaseSource);

                    var updatedBranches = await releaseSourceInstance.StartFetchReleasesAsync().ConfigureAwait(false);
                    await _cloudflare.InvalidateBranches(updatedBranches).ConfigureAwait(false);
                }
            }
            finally
            {
                if (obtainedLock)
                {
                    releaseLock.Release();
                }
            }
        }
    }
}
