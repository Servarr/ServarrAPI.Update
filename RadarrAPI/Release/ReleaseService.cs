using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NLog;
using RadarrAPI.Release.AppVeyor;
using RadarrAPI.Release.Github;
using RadarrAPI.Update;

namespace RadarrAPI.Release
{
    public class ReleaseService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<Branch, ReleaseSourceBase> _releaseBranches;

        public ReleaseService(IServiceProvider serviceProvider)
        {
            _releaseBranches = new ConcurrentDictionary<Branch, ReleaseSourceBase>();
            _releaseBranches.TryAdd(Branch.Develop, new GithubReleaseSource(serviceProvider, Branch.Develop));
            _releaseBranches.TryAdd(Branch.Nightly, new AppVeyorReleaseSource(serviceProvider, Branch.Nightly));
        }

        public void UpdateReleases(Branch branch)
        {
            ReleaseSourceBase releaseSourceBase;

            if (!_releaseBranches.TryGetValue(branch, out releaseSourceBase))
            {
                throw new NotImplementedException($"{branch} does not have a release source.");
            }

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await releaseSourceBase.StartFetchReleasesAsync();
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"UpdateReleases({branch}) threw an exception");
                }
            });
        }
    }
}
