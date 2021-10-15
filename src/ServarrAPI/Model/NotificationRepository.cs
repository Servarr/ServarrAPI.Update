using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ServarrAPI.Datastore;

namespace ServarrAPI.Model
{
    public interface INotificationRepository : IBasicRepository<Notification>
    {
        Task<List<Notification>> Find(string version, string branch, OperatingSystem os, Runtime runtime, Architecture arch);
    }

    public class NotificationRepository : BasicRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(IDatabase database)
        : base(database)
        {
        }

        public async Task<List<Notification>> Find(string version, string branch, OperatingSystem os, Runtime runtime, Architecture arch)
        {
            var all = await All();

            var result2 = all.Where(f => (f.OperatingSystems.Contains(os) || f.OperatingSystems.Count == 0) &&
                                                              (f.Runtimes.Contains(runtime) || f.Runtimes.Count == 0) &&
                                                              (f.Architectures.Contains(arch) || f.Architectures.Count == 0) &&
                                                              (f.Branches.Contains(branch) || f.Branches.Count == 0) &&
                                                              (f.Versions.Contains(version) || f.Versions.Count == 0));
            return result2.ToList();
        }
    }
}
