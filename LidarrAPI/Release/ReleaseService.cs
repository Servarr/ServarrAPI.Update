using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LidarrAPI.Database;
using LidarrAPI.Release.AppVeyor;
using LidarrAPI.Release.Github;
using LidarrAPI.Update;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace LidarrAPI.Release
{
    public class ReleaseService
    {
        private static readonly ConcurrentDictionary<Branch, SemaphoreSlim> ReleaseLocks;

        private readonly IServiceProvider _serviceProvider;
        
        private readonly ConcurrentDictionary<Branch, Type> _releaseBranches;

        static ReleaseService()
        {
            ReleaseLocks = new ConcurrentDictionary<Branch, SemaphoreSlim>();
            ReleaseLocks.TryAdd(Branch.Develop, new SemaphoreSlim(1, 1));
            ReleaseLocks.TryAdd(Branch.Nightly, new SemaphoreSlim(1, 1));
        }

        public ReleaseService(IServiceProvider serviceProvider, DatabaseContext databaseContext)
        {
            _serviceProvider = serviceProvider;

            _releaseBranches = new ConcurrentDictionary<Branch, Type>();
            _releaseBranches.TryAdd(Branch.Develop, typeof(GithubReleaseSource));
            _releaseBranches.TryAdd(Branch.Nightly, typeof(AppVeyorReleaseSource));

        }

        public async Task UpdateReleasesAsync(Branch branch)
        {
            if (!_releaseBranches.TryGetValue(branch, out var releaseSourceBaseType))
            {
                throw new NotImplementedException($"{branch} does not have a release source.");
            }

            if (!ReleaseLocks.TryGetValue(branch, out var releaseLock))
            {
                throw new NotImplementedException($"{branch} does not have a release lock.");
            }

            var obtainedLock = false;

            try
            {
                obtainedLock = await releaseLock.WaitAsync(TimeSpan.FromMinutes(5));

                if (obtainedLock)
                {
                    var releaseSourceInstance = (ReleaseSourceBase) _serviceProvider.GetRequiredService(releaseSourceBaseType);

                    releaseSourceInstance.ReleaseBranch = branch;

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
