using System.Linq;
using System.Threading.Tasks;
using ServarrAPI.Datastore;

namespace ServarrAPI.Model
{
    public interface IUpdateRepository : IBasicRepository<UpdateEntity>
    {
        Task<UpdateEntity> Find(string version, string branch);
    }

    public class UpdateRepository : BasicRepository<UpdateEntity>, IUpdateRepository
    {
        public UpdateRepository(IDatabase database)
        : base(database)
        {
        }

        public async Task<UpdateEntity> Find(string version, string branch)
        {
            var result = await Query(x => x.Version == version && x.Branch == branch)
                .ConfigureAwait(false);
            return result.SingleOrDefault();
        }
    }
}
