using System.Threading;
using System.Threading.Tasks;
using LidarrAPI.Update;

namespace LidarrAPI.Release
{
    public abstract class ReleaseSourceBase
    {
        protected ReleaseSourceBase()
        {
            ReleaseBranch = Branch.Unknown;
            FetchSemaphore = new Semaphore(1, 1);
        }

        public Branch ReleaseBranch { get; set; }
        
        /// <summary>
        ///     Used to have only one thread fetch releases.
        /// </summary>
        private Semaphore FetchSemaphore { get; }

        public async Task<bool> StartFetchReleasesAsync()
        {
            var hasLock = false;

            try
            {
                hasLock = FetchSemaphore.WaitOne();

                if (hasLock)
                {
                    return await DoFetchReleasesAsync();
                }
            }
            finally
            {
                if (hasLock)
                {
                    FetchSemaphore.Release();
                }
            }

            return false;
        }

        protected abstract Task<bool> DoFetchReleasesAsync();
    }
}