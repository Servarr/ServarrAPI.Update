using System.Threading;
using System.Threading.Tasks;

namespace ServarrAPI.Release
{
    public abstract class ReleaseSourceBase
    {
        protected ReleaseSourceBase()
        {
            FetchSemaphore = new Semaphore(1, 1);
        }

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
