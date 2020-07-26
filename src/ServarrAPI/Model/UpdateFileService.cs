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
            runtime = SetRuntime(runtime);
            arch = SetArch(runtime, arch);
            os = SetOs(runtime, os);

            return _repo.Find(version, branch, os, runtime, arch);
        }

        public Task<List<UpdateFileEntity>> Find(string branch, OperatingSystem os, Runtime runtime, Architecture arch, int count)
        {
            runtime = SetRuntime(runtime);
            arch = SetArch(runtime, arch);
            os = SetOs(runtime, os);

            return _repo.Find(branch, os, runtime, arch, count);
        }

        private Runtime SetRuntime(Runtime runtime)
        {
            // Mono and Dotnet are equivalent for our purposes
            if (runtime == Runtime.Mono)
            {
                return Runtime.DotNet;
            }

            return runtime;
        }

        private Architecture SetArch(Runtime runtime, Architecture arch)
        {
            // If runtime is DotNet then default arch to x64
            if (runtime == Runtime.DotNet)
            {
                return Architecture.X64;
            }

            return arch;
        }

        private OperatingSystem SetOs(Runtime runtime, OperatingSystem os)
        {
            // We only care about LinuxMusl and BSD for net core
            if (runtime == Runtime.DotNet &&
                (os == OperatingSystem.LinuxMusl ||
                 os == OperatingSystem.Bsd))
            {
                return OperatingSystem.Linux;
            }

            return os;
        }
    }
}
