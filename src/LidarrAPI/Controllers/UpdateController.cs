using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LidarrAPI.Database;
using LidarrAPI.Database.Models;
using LidarrAPI.Update;
using LidarrAPI.Update.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Architecture = System.Runtime.InteropServices.Architecture;
using OperatingSystem = LidarrAPI.Update.OperatingSystem;

namespace LidarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class UpdateController : Controller
    {
        private readonly DatabaseContext _database;

        public UpdateController(DatabaseContext database)
        {
            _database = database;
        }

        private IQueryable<UpdateFileEntity> GetUpdateFiles(string branch, OperatingSystem os, Runtime runtime, Architecture arch)
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

            Expression<Func<UpdateFileEntity, bool>> predicate;

            // Return whatever runtime/arch for macos and windows
            // Choose correct runtime/arch for linux
            if (os == OperatingSystem.Linux)
            {
                predicate = (x) => x.Update.Branch == branch &&
                    x.OperatingSystem == os &&
                    x.Architecture == arch &&
                    x.Runtime == runtime;
            }
            else
            {
                predicate = (x) => x.Update.Branch == branch &&
                    x.OperatingSystem == os;
            }

            return _database.UpdateFileEntities
                .Include(x => x.Update)
                .Where(predicate)
                .OrderByDescending(x => x.Update.ReleaseDate);
        }

        [Route("{branch}/changes")]
        [HttpGet]
        public object GetChanges(
            [FromRoute(Name = "branch")] string updateBranch,
            [FromQuery(Name = "os")] OperatingSystem operatingSystem,
            [FromQuery(Name = "runtime")] Runtime runtime = Runtime.DotNet,
            [FromQuery(Name = "arch")] Architecture arch = Architecture.X64
            )
        {
            var updateFiles = GetUpdateFiles(updateBranch, operatingSystem, runtime, arch).Take(5);

            var response = new List<UpdatePackage>();

            foreach (var updateFile in updateFiles)
            {
                var update = updateFile.Update;
                UpdateChanges updateChanges = null;

                if (update.New.Count != 0 || update.Fixed.Count != 0)
                {
                    updateChanges = new UpdateChanges
                    {
                        New = update.New,
                        Fixed = update.Fixed
                    };
                }

                response.Add(new UpdatePackage
                    {
                        Version = update.Version,
                        ReleaseDate = update.ReleaseDate,
                        Filename = updateFile.Filename,
                        Url = updateFile.Url,
                        Changes = updateChanges,
                        Hash = updateFile.Hash,
                        Status = update.Status,
                        Branch = update.Branch.ToString().ToLower()
                    });
            }

            return response;
        }

        [Route("{branch}")]
        [HttpGet]
        public object GetUpdates([FromRoute(Name = "branch")] string updateBranch,
                                 [FromQuery(Name = "version")] string urlVersion,
                                 [FromQuery(Name = "os")] OperatingSystem operatingSystem,
                                 [FromQuery(Name = "runtime")] Runtime runtime,
                                 [FromQuery(Name = "arch")] Architecture arch)
        {
            // Check given version
            if (!Version.TryParse(urlVersion, out var version))
            {
                return new
                {
                    ErrorMessage = "Invalid version number specified."
                };
            }

            var updateFile = GetUpdateFiles(updateBranch, operatingSystem, runtime, arch).FirstOrDefault();

            if (updateFile == null)
            {
                return new UpdatePackageContainer
                {
                    Available = false
                };
            }

            var update = updateFile.Update;

            // Compare given version and update version
            var updateVersion = new Version(update.Version);
            if (updateVersion.CompareTo(version) <= 0)
            {
                return new UpdatePackageContainer
                {
                    Available = false
                };
            }

            // Get the update changes
            UpdateChanges updateChanges = null;

            if (update.New.Count != 0 || update.Fixed.Count != 0)
            {
                updateChanges = new UpdateChanges
                {
                    New = update.New,
                    Fixed = update.Fixed
                };
            }

            return new UpdatePackageContainer
            {
                Available = true,
                UpdatePackage = new UpdatePackage
                {
                    Version = update.Version,
                    ReleaseDate = update.ReleaseDate,
                    Filename = updateFile.Filename,
                    Url = updateFile.Url,
                    Changes = updateChanges,
                    Hash = updateFile.Hash,
                    Branch = update.Branch.ToString().ToLower(),
                    Runtime = updateFile.Runtime.ToString().ToLower()
                }
            };
        }

        [Route("{branch}/updatefile")]
        [HttpGet]
        public object GetUpdateFile([FromRoute(Name = "branch")] string updateBranch,
                                    [FromQuery(Name = "version")] string urlVersion,
                                    [FromQuery(Name = "os")] OperatingSystem operatingSystem,
                                    [FromQuery(Name = "runtime")] Runtime runtime,
                                    [FromQuery(Name = "arch")] Architecture arch)
        {
            UpdateFileEntity updateFile;

            if (urlVersion != null)
            {
                if (!Version.TryParse(urlVersion, out Version version))
                {
                    return new
                        {
                            ErrorMessage = "Invalid version number specified."
                        };
                }

                updateFile = GetUpdateFiles(updateBranch, operatingSystem, runtime, arch).FirstOrDefault(x => x.Update.Version == version.ToString());
            }
            else
            {
                updateFile = GetUpdateFiles(updateBranch, operatingSystem, runtime, arch).FirstOrDefault();
            }

            if (updateFile == null)
            {
                return new
                    {
                        ErrorMessage = $"Update file for {updateBranch}-{urlVersion} not found."
                    };
            }

            return RedirectPermanent(updateFile.Url);
        }
    }
}
