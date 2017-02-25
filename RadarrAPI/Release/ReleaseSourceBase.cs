using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RadarrAPI.Update;

namespace RadarrAPI.Release
{
    public abstract class ReleaseSourceBase
    {
        /// <summary>
        ///     Used to have only one thread fetch releases.
        /// </summary>
        private readonly Semaphore _fetchSemaphore;

        protected ReleaseSourceBase(IServiceProvider serviceProvider, Branch branch)
        {
            ServiceProvider = serviceProvider;
            ReleaseBranch = branch;
            Config = ServiceProvider.GetService<IOptions<Config>>().Value;
            
            _fetchSemaphore = new Semaphore(1, 1);
        }

        public IServiceProvider ServiceProvider { get; set; }

        public Branch ReleaseBranch { get; }

        public Config Config { get; set; }

        public async Task StartFetchReleasesAsync()
        {
            var hasLock = false;

            try
            {
                hasLock = _fetchSemaphore.WaitOne();

                if (hasLock)
                {
                    await DoFetchReleasesAsync();
                }
            }
            finally
            {
                if (hasLock)
                {
                    _fetchSemaphore.Release();
                }
            }
        }

        protected abstract Task DoFetchReleasesAsync();
    }
}