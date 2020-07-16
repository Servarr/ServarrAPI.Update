using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ServarrAPI.Model
{
    public interface IUpdateService
    {
        Task<UpdateEntity> Insert(UpdateEntity entity);
        Task<UpdateEntity> Find(string version, string branch);
    }

    public class UpdateService : IUpdateService
    {
        private readonly ILogger _logger;
        private readonly IUpdateRepository _repo;

        public UpdateService(IUpdateRepository repo,
                             ILogger<IUpdateService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<UpdateEntity> Insert(UpdateEntity entity)
        {
            return _repo.Insert(entity);
        }

        public Task<UpdateEntity> Find(string version, string branch)
        {
            return _repo.Find(version, branch);
        }
    }
}
