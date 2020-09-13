using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ServarrAPI.Datastore;
using ServarrAPI.Extensions;

namespace ServarrAPI.Model
{
    public interface IUpdateFileRepository : IBasicRepository<UpdateFileEntity>
    {
        Task<UpdateFileEntity> Find(string version, string branch, OperatingSystem os, Runtime runtime, Architecture arch);
        Task<List<UpdateFileEntity>> Find(string branch, OperatingSystem os, Runtime runtime, Architecture arch, int count, Version maxVersion);
    }

    public class UpdateFileRepository : BasicRepository<UpdateFileEntity>, IUpdateFileRepository
    {
        public UpdateFileRepository(IDatabase database)
        : base(database)
        {
        }

        public async Task<UpdateFileEntity> Find(string version, string branch, OperatingSystem os, Runtime runtime, Architecture arch)
        {
            var result = await Query(Builder()
                                     .Where<UpdateFileEntity>(f => f.OperatingSystem == os &&
                                                              f.Runtime == runtime &&
                                                              f.Architecture == arch)
                                     .Where<UpdateEntity>(u => u.Branch == branch &&
                                                               u.Version == version));
            return result.FirstOrDefault();
        }

        public async Task<List<UpdateFileEntity>> Find(string branch, OperatingSystem os, Runtime runtime, Architecture arch, int count, Version maxVersion)
        {
            var builder = Builder().Where<UpdateFileEntity>(f => f.OperatingSystem == os);

            if (os == OperatingSystem.Linux ||
                os == OperatingSystem.LinuxMusl)
            {
                builder.Where<UpdateFileEntity>(f => f.Runtime == runtime && f.Architecture == arch);
            }

            if (maxVersion != null)
            {
                var maxVersionInt = maxVersion.ToIntVersion();
                builder.Where<UpdateEntity>(u => u.IntVersion <= maxVersionInt);
            }

            var result = await Query(builder.Where<UpdateEntity>(u => u.Branch == branch).OrderBy($"update.intversion DESC LIMIT {count}"))
                .ConfigureAwait(false);

            return result.ToList();
        }

        protected override SqlBuilder Builder()
        {
            return new SqlBuilder()
                .Join<UpdateFileEntity, UpdateEntity>((f, u) => f.UpdateId == u.Id);
        }

        protected override async Task<List<UpdateFileEntity>> Query(SqlBuilder builder)
        {
            var result = await _database.QueryJoined<UpdateFileEntity, UpdateEntity>(builder,
                                                                                     (file, update) =>
                                                                                     {
                                                                                         file.Update = update;
                                                                                         return file;
                                                                                     });
            return result.ToList();
        }
    }
}
