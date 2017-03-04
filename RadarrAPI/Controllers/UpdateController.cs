using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadarrAPI.Database;
using RadarrAPI.Update;
using RadarrAPI.Update.Data;
using StatsdClient;
using Branch = RadarrAPI.Update.Branch;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class UpdateController : Controller
    {
        private readonly DatabaseContext _database;

        public UpdateController(DatabaseContext database)
        {
            _database = database;
        }

        [Route("{branch}/changes")]
        [HttpGet]
        public object GetChanges([FromRoute(Name = "branch")]Branch updateBranch, [FromQuery(Name = "version")]string urlVersion, [FromQuery(Name = "os")]OperatingSystem operatingSystem)
        {
            using (DogStatsd.StartTimer("controller.update.get_changes.time"))
            {
                DogStatsd.Increment("controller.update.get_changes.count");

                var updates = _database.UpdateEntities
                .Include(x => x.UpdateFiles)
                .Where(x => x.Branch == updateBranch && x.UpdateFiles.Any(u => u.OperatingSystem == operatingSystem))
                .OrderByDescending(x => x.ReleaseDate)
                .Take(5);

                var response = new List<UpdatePackage>();

                foreach (var update in updates)
                {
                    var updateFile = update.UpdateFiles.FirstOrDefault(u => u.OperatingSystem == operatingSystem);
                    if (updateFile == null) continue;

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
                        Branch = update.Branch.ToString().ToLower()
                    });
                }

                return response;
            }
        }

        [Route("{branch}")]
        [HttpGet]
        public object GetUpdates([FromRoute(Name = "branch")]Branch updateBranch, [FromQuery(Name = "version")]string urlVersion, [FromQuery(Name = "os")]OperatingSystem operatingSystem)
        {
            // Check given version
            if (!Version.TryParse(urlVersion, out Version version))
            {
                return new
                {
                    ErrorMessage = "Invalid version number specified."
                };
            }

            using (DogStatsd.StartTimer("controller.update.get_updates.time"))
            {
                DogStatsd.Increment("controller.update.get_updates.count");
                
                // Grab latest update based on branch and operatingsystem
                var update = _database.UpdateEntities
                    .Include(x => x.UpdateFiles)
                    .Where(x => x.Branch == updateBranch && x.UpdateFiles.Any(u => u.OperatingSystem == operatingSystem))
                    .OrderByDescending(x => x.ReleaseDate)
                    .Take(1)
                    .FirstOrDefault();

                if (update == null)
                {
                    return new
                    {
                        ErrorMessage = "Latest update not found."
                    };
                }

                // Check if update file is present
                var updateFile = update.UpdateFiles.FirstOrDefault(u => u.OperatingSystem == operatingSystem);
                if (updateFile == null)
                {
                    return new
                    {
                        ErrorMessage = "Latest update file not found."
                    };
                }

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
                        Branch = update.Branch.ToString().ToLower()
                    }
                };
            }
        }
    }
}
