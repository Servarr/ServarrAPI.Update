using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ServarrAPI.Release.Azure;
using ServarrAPI.Release.Github;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServarrAPI.Release
{
    public class ReleaseService
    {
        private static readonly ConcurrentDictionary<Type, SemaphoreSlim> ReleaseLocks;

        private readonly IServiceProvider _serviceProvider;

        static ReleaseService()
        {
            ReleaseLocks = new ConcurrentDictionary<Type, SemaphoreSlim>();
            ReleaseLocks.TryAdd(typeof(GithubReleaseSource), new SemaphoreSlim(1, 1));
            ReleaseLocks.TryAdd(typeof(AzureReleaseSource), new SemaphoreSlim(1, 1));
        }

        public ReleaseService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
                obtainedLock = await releaseLock.WaitAsync(TimeSpan.FromMinutes(5));

                if (obtainedLock)
                {
                    var releaseSourceInstance = (ReleaseSourceBase) _serviceProvider.GetRequiredService(releaseSource);

                    await releaseSourceInstance.StartFetchReleasesAsync();
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
