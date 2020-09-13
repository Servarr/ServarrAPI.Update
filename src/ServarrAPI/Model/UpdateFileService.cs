using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServarrAPI.Model
{
    public interface IUpdateFileService
    {
        Task<UpdateFileEntity> Insert(UpdateFileEntity model);
        Task<UpdateFileEntity> Find(string version, string branch, OperatingSystem os, Runtime runtime, Architecture arch);
        Task<List<UpdateFileEntity>> Find(string branch, OperatingSystem os, Runtime runtime, Architecture arch, int count, string installedVersion = null);
    }

    public class UpdateFileService : IUpdateFileService
    {
        private readonly ILogger _logger;
        private readonly IUpdateFileRepository _repo;
        private readonly Config _config;

        public UpdateFileService(IUpdateFileRepository repo,
                                 ILogger<IUpdateFileService> logger,
                                 IOptions<Config> optionsConfig)
        {
            _repo = repo;
            _logger = logger;
            _config = optionsConfig.Value;
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

        public Task<List<UpdateFileEntity>> Find(string branch, OperatingSystem os, Runtime runtime, Architecture arch, int count, string installedVersion = null)
        {
            runtime = SetRuntime(runtime);
            arch = SetArch(runtime, arch);
            os = SetOs(runtime, os);
            var maxVersion = GetMaxVersion(installedVersion);

            return _repo.Find(branch, os, runtime, arch, count, maxVersion);
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

        private Version GetMaxVersion(string currentVersion)
        {
            if (!string.IsNullOrWhiteSpace(currentVersion))
            {
                var installedVersion = new Version(currentVersion);
                return _config.VersionGates.OrderBy(x => x.MaxVersion).FirstOrDefault(x => installedVersion <= x.MaxVersion)?.MaxUpgradeVersion;
            }
            else
            {
                return null;
            }
        }
    }
}
