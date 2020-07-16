using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ServarrAPI.Model
{
    public interface IUpdateFileService
    {
        Task<UpdateFileEntity> Insert(UpdateFileEntity model);
        Task<UpdateFileEntity> Find(string version, string branch, OperatingSystem os, Runtime runtime, Architecture arch);
        Task<List<UpdateFileEntity>> Find(string branch, OperatingSystem os, Runtime runtime, Architecture arch, int count);
    }

    public class UpdateFileService : IUpdateFileService
    {
        private readonly ILogger _logger;
        private readonly IUpdateFileRepository _repo;

        public UpdateFileService(IUpdateFileRepository repo,
                                 ILogger<IUpdateFileService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<UpdateFileEntity> Insert(UpdateFileEntity model)
        {
            return _repo.Insert(model);
        }

        public Task<UpdateFileEntity> Find(string version, string branch, OperatingSystem os, Runtime runtime, Architecture arch)
        {
            return _repo.Find(version, branch, os, runtime, arch);
        }

        public Task<List<UpdateFileEntity>> Find(string branch, OperatingSystem os, Runtime runtime, Architecture arch, int count)
        {
            // Mono and Dotnet are equivalent for our purposes
            if (runtime == Runtime.Mono)
            {
                runtime = Runtime.DotNet;
            }

            // If runtime is DotNet then default arch to x64
            if (runtime == Runtime.DotNet)
            {
                arch = Architecture.X64;
            }

            return _repo.Find(branch, os, runtime, arch, count);
        }
    }
}
