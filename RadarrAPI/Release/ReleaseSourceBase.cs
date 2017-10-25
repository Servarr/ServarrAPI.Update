using System.Threading;
using System.Threading.Tasks;
using RadarrAPI.Update;

namespace RadarrAPI.Release
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

        public async Task StartFetchReleasesAsync()
        {
            var hasLock = false;

            try
            {
                hasLock = FetchSemaphore.WaitOne();

                if (hasLock)
                {
                    await DoFetchReleasesAsync();
                }
            }
            finally
            {
                if (hasLock)
                {
                    FetchSemaphore.Release();
                }
            }
        }

        protected abstract Task DoFetchReleasesAsync();
    }
}